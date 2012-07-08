using System;
using System.ComponentModel;
using NotifyPropertyWeaver;

public class ClassWithBranchingReturnAndBeforeAfter : INotifyPropertyChanged
{
    string property1;
    bool isInSomeMode;
    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyProperty]
    public string Property1
    {
        get { return property1; }
        set
        {
            property1 = value;
            if (isInSomeMode)
            {
                Console.WriteLine("code here so 'if' does not get optimized away in release mode");
                return;
            }
        }
    }

    public void OnPropertyChanged(string propertyName, object before, object after)
    {
        var handler = PropertyChanged;
        if (handler != null)
        {
            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
