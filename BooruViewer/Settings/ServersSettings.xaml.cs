using System.ComponentModel;
using System.Windows.Controls;

namespace BooruViewer.Settings
{
    /// <summary>
    /// Логика взаимодействия для ServersSettings.xaml
    /// </summary>
    public partial class ServersSettings : Grid, ISettingsPage
    {
        public bool IsEdited => Model.IsEdited;
        public bool CanClose => Model.CanClose;
        public bool Apply() => Model.Apply();
        public void Reset() => Model.Reset();

        public ServerSettingsViewModel Model => DataContext as ServerSettingsViewModel;

        public ServersSettings()
        {
            InitializeComponent();
            DataContext = new ServerSettingsViewModel();
        }
    }
}
