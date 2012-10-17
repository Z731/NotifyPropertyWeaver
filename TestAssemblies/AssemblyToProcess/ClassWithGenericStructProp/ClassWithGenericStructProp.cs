using System.ComponentModel;
using NotifyPropertyWeaver;

[NotifyForAll]
public class ClassWithGenericStructProp<T> : INotifyPropertyChanged where T : struct
{
    public T? Property1 { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
}