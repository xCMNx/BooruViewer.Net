using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Booru.Core;

namespace BooruViewer.Settings
{
    public partial class LoaderSettings : Grid
    {
        public LoaderSettingsViewModel Model => DataContext as LoaderSettingsViewModel;
        public LoaderSettings()
        {
            InitializeComponent();
            DataContextChanged += LoaderSettings_DataContextChanged;
            CommandBinding delete = new CommandBinding(ApplicationCommands.Delete);
            delete.Executed += Delete_Executed;
            this.CommandBindings.Add(delete);
        }

        private void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Model?.Delete((IDataLoader)e.Parameter);
        }

        private void LoaderSettings_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext != null)
                Model.PropertyChanged -= Model_PropertyChanged;
            var mdl = e.NewValue as LoaderSettingsViewModel;
            if (mdl != null)
                mdl.PropertyChanged += Model_PropertyChanged;
        }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoaderSettingsViewModel.Editor))
                ContainerSettings.Child = Model.Editor;
        }
    }
}
