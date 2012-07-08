﻿using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Mono.Cecil;
using NotifyPropertyWeaverMsBuildTask;

[Export]
public class PropertyDataWalker
{
    TypeNodeBuilder typeNodeBuilder;
    NotifyPropertyDataAttributeReader notifyPropertyDataAttributeReader;
    Logger logger;
    WeavingTask weavingTask;

    [ImportingConstructor]
    public PropertyDataWalker(TypeNodeBuilder typeNodeBuilder, NotifyPropertyDataAttributeReader notifyPropertyDataAttributeReader, Logger logger, WeavingTask weavingTask)
    {
        this.typeNodeBuilder = typeNodeBuilder;
        this.notifyPropertyDataAttributeReader = notifyPropertyDataAttributeReader;
        this.logger = logger;
        this.weavingTask = weavingTask;
    }

    void Process(List<TypeNode> notifyNodes)
    {
        foreach (var node in notifyNodes)
        {
            foreach (var property in node.TypeDefinition.Properties)
            {
                if (property.CustomAttributes.ContainsAttribute("DoNotNotifyAttribute"))
                {
                    continue;
                }
                var containsAttribute = property.CustomAttributes.ContainsAttribute("NotifyPropertyAttribute");
                if (property.SetMethod == null)
                {
                    if (containsAttribute)
                    {
                        logger.LogMessage(string.Format("\t\t'{0}' skipped because could not find a field set in the property set code. Are you missing code to set the field?", property.GetName()));
                    }
                    continue;
                }

                if (property.SetMethod.IsStatic)
                {
                    continue;
                }
                if (!containsAttribute && !(node.ShouldNotifyForAllInType || weavingTask.TryToWeaveAllTypes))
                {
                    continue;
                }
                GetPropertyData(property, node);
            }
            Process(node.Nodes);
        }
    }

    void GetPropertyData(PropertyDefinition propertyDefinition, TypeNode node)
    {
        var notifyPropertyData = notifyPropertyDataAttributeReader.Read(propertyDefinition, node.AllProperties);
        var dependenciesForProperty = node.PropertyDependencies.Where(x => x.WhenPropertyIsSet == propertyDefinition).Select(x => x.ShouldAlsoNotifyFor);

        var backingFieldReference = node.Mappings.First(x => x.PropertyDefinition == propertyDefinition).FieldDefinition;
        if (notifyPropertyData == null)
        {
            if (node.EventInvoker == null)
            {
                return;
            }
            node.PropertyDatas.Add(new PropertyData
                                       {
                                           CheckForEquality = weavingTask.CheckForEquality,
                                           SetIsChanged = weavingTask.CheckForIsChanged,
                                           BackingFieldReference = backingFieldReference,
                                           NotificationAddedDirectly = false,
                                           PropertyDefinition = propertyDefinition,
                                           // Compute full dependencies for the current property
                                           AlsoNotifyFor = GetFullDependencies(propertyDefinition, dependenciesForProperty, node)
                                       });
            return;
        }

        if (node.EventInvoker == null)
        {
            throw new WeavingException(string.Format(
                @"Could not find field for PropertyChanged event on type '{0}'.
Looked for 'PropertyChanged', 'propertyChanged', '_PropertyChanged' and '_propertyChanged'.
The most likely cause is that you have implemented a custom event accessor for the PropertyChanged event and have called the PropertyChangedEventHandler something stupid.", node.TypeDefinition.FullName));
        }
        node.PropertyDatas.Add(new PropertyData
                                   {
                                       CheckForEquality = notifyPropertyData.CheckForEquality.GetValueOrDefault(weavingTask.CheckForEquality),
                                       BackingFieldReference = backingFieldReference,
                                       NotificationAddedDirectly = true,
                                       PropertyDefinition = propertyDefinition,
                                       // Compute full dependencies for the current property
                                       AlsoNotifyFor = GetFullDependencies(propertyDefinition, notifyPropertyData.AlsoNotifyFor.Union(dependenciesForProperty), node),
                                       SetIsChanged = notifyPropertyData.SetIsChanged.GetValueOrDefault(weavingTask.CheckForIsChanged),
                                   });
    }

    List<PropertyDefinition> GetFullDependencies(PropertyDefinition propertyDefinition, IEnumerable<PropertyDefinition> dependenciesForProperty, TypeNode node)
    {
        // Create an HashSet to contain all dependent properties (direct or transitive)
        // Add the initial Property to stop the recursion if this property is a dependency of another property
        var fullDependencies = new HashSet<PropertyDefinition> {propertyDefinition};

        foreach (var dependentProperty in dependenciesForProperty)
        {
            // Check if the property is already present in the HashSet before starting the recursion
            if (fullDependencies.Add(dependentProperty))
            {
                ComputeDependenciesRec(dependentProperty, fullDependencies, node);
            }
        }

        // Remove the initial Property of the HashSet.
        fullDependencies.Remove(propertyDefinition);

        return fullDependencies.ToList();
    }

    /// <summary>
    /// Computes dependencies recursively
    /// </summary>
    void ComputeDependenciesRec(PropertyDefinition propertyDefinition, HashSet<PropertyDefinition> fullDependencies, TypeNode node)
    {
        // TODO: An optimization could be done to avoid the multiple computation of one property for each property of the type
        // By keeping the in memory the full dependencies of each property of the type

        foreach (var dependentProperty in node.PropertyDependencies.Where(x => x.WhenPropertyIsSet == propertyDefinition).Select(x => x.ShouldAlsoNotifyFor))
        {
            if (fullDependencies.Contains(dependentProperty))
            {
                continue;
            }
            fullDependencies.Add(dependentProperty);

            ComputeDependenciesRec(dependentProperty, fullDependencies, node);
        }
    }


    public void Execute()
    {
        Process(typeNodeBuilder.NotifyNodes);
    }
}