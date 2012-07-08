using System.ComponentModel;
using NotifyPropertyWeaver;

public class ClassEqualityWithStructOverload : INotifyPropertyChanged
{
    [NotifyProperty(PerformEqualityCheck = true)]
    public SimpleStruct Property1 { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
    
    public struct SimpleStruct
    {

        public int X ;
        public static bool operator ==(SimpleStruct left, SimpleStruct right)
        {
            return left.X == right.X;
        }

        public static bool operator !=(SimpleStruct left, SimpleStruct right)
        {
            return !(left == right);
        }
    }

}