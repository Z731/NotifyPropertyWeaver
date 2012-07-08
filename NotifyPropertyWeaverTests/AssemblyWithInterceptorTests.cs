﻿using System.Reflection;
using NUnit.Framework;


[TestFixture]
public class AssemblyWithInterceptorTests
{
    [Test]
    public void Simple()
    {
        var weaverHelper = new WeaverHelper(@"AssemblyWithInterceptor\AssemblyWithInterceptor.csproj", true);

        var assembly = weaverHelper.Assembly;
        var instance = assembly.GetInstance("ClassToTest");
        EventTester.TestProperty(instance, false);
        var type = assembly.GetType("PropertyNotificationInterceptor");
        var propertyInfo = type.GetProperty("InterceptCalled", BindingFlags.Static | BindingFlags.Public);
        var value = (bool)propertyInfo.GetValue(null, null);
        Assert.IsTrue(value);
        Verifier.Verify(assembly.CodeBase.Replace("file:///", string.Empty));

    }
    [Test]
    public void BeforeAfter()
    {
        var weaverHelper = new WeaverHelper(@"AssemblyWithBeforeAfterInterceptor\AssemblyWithBeforeAfterInterceptor.csproj", true);
        var assembly = weaverHelper.Assembly;
        var instance = assembly.GetInstance("ClassToTest");
        EventTester.TestProperty(instance, false);
        var type = assembly.GetType("PropertyNotificationInterceptor");
        var propertyInfo = type.GetProperty("InterceptCalled", BindingFlags.Static | BindingFlags.Public);
        var value = (bool)propertyInfo.GetValue(null, null);
        Assert.IsTrue(value);
        Verifier.Verify(assembly.CodeBase.Replace("file:///", string.Empty));
    }
}