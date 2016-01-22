using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Booru.Core;

namespace BooruViewer.Settings
{
    /// <summary>
    /// Логика взаимодействия для DataContainerSettings.xaml
    /// </summary>
    public partial class DataContainerSettings : Grid, ISettingsPage, INotifyPropertyChanged
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
        public Type Selected
        {
            get { return _Selected; }
            set
            {
                try
                {
                    SettingsEditor = value?.SettingsEditor();
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

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected bool _IsEdited = false;
        public bool IsEdited => _IsEdited;
        public bool CanClose => !_IsEdited && Core.DataContainer != null;

        public DataContainerSettings()
        {
            InitializeComponent();
            Selected = Core.DataContainerType;
            _IsEdited = false;
        }

        public bool Apply()
        {
            try
            {
                var settings = _editor?.Settings;
                var _tmp = (IDataContainer)Activator.CreateInstance(_Selected, settings);
                Core.SaveSettings(settings);
                if (Core.DataContainer != null)
                    MessageBox.Show("To apply changes, you must restart program.");
                Core.DataContainer = _tmp;
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
