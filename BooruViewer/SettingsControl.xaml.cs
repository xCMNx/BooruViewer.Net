using System.Windows.Controls;
using System.Windows.Input;
using BooruViewer.Settings;

namespace BooruViewer
{
    /// <summary>
    /// Логика взаимодействия для UserControl1.xaml
    /// </summary>
    public partial class SettingsControl : Grid
    {
        public SettingsControl()
        {
            InitializeComponent();

            CommandBinding MenuClick = new CommandBinding(SettingsCommands.OpenMenu);
            MenuClick.Executed += MenuClick_Executed;
            MenuClick.CanExecute += MenuClick_CanExecute;
            this.CommandBindings.Add(MenuClick);

            CommandBinding ApplyClick = new CommandBinding(SettingsCommands.Apply);
            ApplyClick.Executed += ApplyClick_Executed;
            ApplyClick.CanExecute += ApplyClick_CanExecute;
            this.CommandBindings.Add(ApplyClick);

            CommandBinding ResetClick = new CommandBinding(SettingsCommands.Reset);
            ResetClick.Executed += ResetClick_Executed;
            ResetClick.CanExecute += ResetClick_CanExecute;
            this.CommandBindings.Add(ResetClick);

            /*
			CommandBinding CloseClick = new CommandBinding(SettingsCommands.Close);
			CloseClick.Executed += CloseClick_Executed;
			CloseClick.CanExecute += CloseClick_CanExecute;
			this.CommandBindings.Add(CloseClick);
			*/
        }

        private void CloseClick_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ElementView.Child == null || !((ISettingsPage)ElementView.Child).IsEdited;
        }

        private void CloseClick_Executed(object sender, ExecutedRoutedEventArgs e)
        {
        }

        private void ResetClick_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ElementView.Child != null && ((ISettingsPage)ElementView.Child).IsEdited;
        }

        private void ResetClick_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            (ElementView.Child as ISettingsPage)?.Reset();
        }

        private void ApplyClick_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ElementView.Child != null && ((ISettingsPage)ElementView.Child).IsEdited;
        }

        private void ApplyClick_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            (ElementView.Child as ISettingsPage)?.Apply();
        }

        private void MenuClick_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ElementView.Child == null || !((ISettingsPage)ElementView.Child).IsEdited;
        }

        private void MenuClick_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            switch (e.Parameter as string)
            {
                case "DC": ElementView.Child = new DataContainerSettings(); break;
                case "DM": ElementView.Child = new DownloadManagerSettings(); break;
                case "S": ElementView.Child = new ServersSettings(); break;
                case "P": ElementView.Child = new PreviewsSettings(); break;
                default: ElementView.Child = null; break;
            }

        }
    }
}
