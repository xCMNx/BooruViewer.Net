using System.Windows;
using Booru.Core;

namespace BooruViewer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public CoreNotifier CoreNotifier { get; } = Core.Notifier;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            cbSettings.IsChecked = !(cbSettings.IsEnabled = IsSettingsConfigured);
            Core.PropertyChanged += Core_PropertyChanged;
        }

        private void Core_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.PropertyName) || e.PropertyName == nameof(Core.DataContainer) || e.PropertyName == nameof(Core.DataLoadManager) || e.PropertyName == nameof(Core.PreviewsContainer))
                cbSettings.IsEnabled = IsSettingsConfigured;
        }

        public bool IsSettingsConfigured => Core.DataContainer != null && Core.DataLoadManager != null && Core.PreviewsContainer != null;

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Core.Terminate();
        }
    }
}
