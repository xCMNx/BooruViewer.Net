using System;
using System.ComponentModel;
using System.Windows.Controls;
using Booru.Core;

namespace Booru.Base.PageGenerators
{
    [SettingsType(typeof(PageEnumeratorSettings)), EditorControlType(ControlType.Xaml)]
    public partial class xPageEnumeratorEditor : UserControl, IPageGeneratorSettingsEditor
    {

        public IModuleSettings Settings => (DataContext as IModuleSettings).Clone();

        protected Uri _Host;
        public Uri Host
        {
            get
            {
                return _Host;
            }
            set
            {
                _Host = value;
                NotifyPropertyChanged(nameof(Host));
            }
        }

        public xPageEnumeratorEditor(IModuleSettings CurrentSettings)
        {
            InitializeComponent();
            original = CurrentSettings;
            DataContext = current = CurrentSettings?.Clone() ?? new PageEnumeratorSettings();
            ((PageEnumeratorSettings)current).PropertyChanged += XPageEnumeratorEditor_PropertyChanged;
        }

        private void XPageEnumeratorEditor_PropertyChanged(object sender, PropertyChangedEventArgs e)
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
