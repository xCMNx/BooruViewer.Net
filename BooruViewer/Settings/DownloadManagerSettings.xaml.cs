using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Booru.Core;

namespace BooruViewer.Settings
{
    public partial class DownloadManagerSettings : Grid, ISettingsPage, INotifyPropertyChanged
    {
        IModuleSettingsEditor _editor = null;
        public IModuleSettingsEditor SettingsEditor
        {
            get { return _editor; }
            set
            {
                _editor = value;
                NotifyPropertyChanged(nameof(SettingsEditor));
            }
        }

        Type _Selected;
        ObservableCollection<IDataLoader> DataLoaders;

        public Type Selected
        {
            get { return _Selected; }
            set
            {
                if (LoadersSettings.Model != null)
                    LoadersSettings.Model.PropertyChanged -= LoaderSettings_PropertyChanged;
                LoadersSettings.DataContext = null;
                if (DataLoaders != null)
                    DataLoaders.CollectionChanged -= DataLoaders_CollectionChanged;
                try
                {
                    if (value != null)
                    {
                        SettingsEditor = value?.SettingsEditor();
                        DataLoaders = new ObservableCollection<IDataLoader>(Core.ReadLoaders(value));
                        LoadersSettings.DataContext = new LoaderSettingsViewModel(DataLoaders);
                        LoadersSettings.Model.PropertyChanged += LoaderSettings_PropertyChanged;
                        DataLoaders.CollectionChanged += DataLoaders_CollectionChanged;
                    }
                    _Selected = value;
                    _IsEdited = true;
                    NotifyPropertyChanged(nameof(Selected));
                    CommandManager.InvalidateRequerySuggested();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        private void LoaderSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Loader")
            {
                _IsEdited = true;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void DataLoaders_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            _IsEdited = true;
            CommandManager.InvalidateRequerySuggested();
        }

        protected bool _IsEdited = false;
        public bool IsEdited => _IsEdited;
        public bool CanClose => !_IsEdited && Core.DataLoadManager != null;

        public DownloadManagerSettings()
        {
            InitializeComponent();
            Selected = Core.DataLoadManagerType;
            _IsEdited = false;
        }

        public bool Apply()
        {
            try
            {
                var settings = (IManagerSettings)_editor?.Settings;
                var oldsettings = Core.DataLoadManager?.CurrentSettings;
                Core.DataLoadManager = (IDataLoadManager)Activator.CreateInstance(_Selected, settings);
                if (Core.DataLoadManager != null && settings != null && settings.CompareTo(oldsettings) != 0)
                    MessageBox.Show("To apply changes, you must restart program.");
                else
                {
                    Core.DataLoadManager.ApplySettings(settings);
                    Core.AddLoaders(DataLoaders);
                }
                Core.SaveSettings(settings);
                Core.WriteLoaders(DataLoaders, _Selected);
                _IsEdited = false;
                CommandManager.InvalidateRequerySuggested();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return false;
            }
        }

        public void Reset()
        {
            Containers.SelectedItem = null;
            Containers.SelectedItem = Core.DataContainerType;
            _IsEdited = false;
            CommandManager.InvalidateRequerySuggested();
        }


    }
}
