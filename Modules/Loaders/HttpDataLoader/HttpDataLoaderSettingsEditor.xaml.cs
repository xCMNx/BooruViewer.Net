using System.ComponentModel;
using System.Windows.Controls;
using Booru.Core;

namespace Booru.Base.Loader
{
    /// <summary>
    /// Логика взаимодействия для DataLoaderSettingsEditor.xaml
    /// </summary>
    [SettingsType(typeof(HttpDataLoaderSettings)), EditorControlType(ControlType.Xaml)]
    public partial class HttpDataLoaderSettingsEditor : TextBox, IModuleSettingsEditor
    {
        public IModuleSettings Settings => new HttpDataLoaderSettings() { Server = Text };

        public HttpDataLoaderSettingsEditor(HttpDataLoaderSettings Settings)
        {
            InitializeComponent();
            Text = original = Settings?.Server;
            TextChanged += DataLoaderSettingsEditor_TextChanged;
        }

        private void DataLoaderSettingsEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            NotifyPropertyChanged("");
        }

        string original;
        public bool Changed => Text != original;

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
