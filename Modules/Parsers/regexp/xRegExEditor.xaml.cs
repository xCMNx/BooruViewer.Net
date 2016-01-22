using System.ComponentModel;
using System.Windows.Controls;
using Booru.Core;

namespace Booru.Base.Parsers
{
    /// <summary>
    /// Логика взаимодействия для xRegExEditor.xaml
    /// </summary>
    [SettingsType(typeof(RegExSettings)), EditorControlType(ControlType.Xaml)]
    public partial class xRegExEditor : Grid, IModuleSettingsEditor
    {
        public IModuleSettings Settings => (DataContext as IModuleSettings).Clone();
        public xRegExEditor(IModuleSettings CurrentSettings)
        {
            InitializeComponent();
            original = CurrentSettings;
            DataContext = current = CurrentSettings?.Clone() ?? new RegExSettings();
            (current as RegExSettings).PropertyChanged += XRegExEditor_PropertyChanged;
        }

        private void XRegExEditor_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(e.PropertyName);
        }

        IModuleSettings original;
        IModuleSettings current;
        public bool Changed => current.CompareTo(original) != 0;

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
