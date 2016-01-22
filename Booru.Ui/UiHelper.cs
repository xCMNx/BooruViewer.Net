using System.ComponentModel;

namespace Booru.Ui
{
    public static class UiHelper
    {
        public static void ChangeObject<T>(ref T DstVar, PropertyChangedEventHandler Func, T newVal) where T : INotifyPropertyChanged
        {
            if (DstVar != null)
                DstVar.PropertyChanged -= Func;
            DstVar = newVal;
            if (DstVar != null)
                DstVar.PropertyChanged += Func;
        }
    }
}
