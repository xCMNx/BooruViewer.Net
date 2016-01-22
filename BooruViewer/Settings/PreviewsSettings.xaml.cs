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
    public partial class PreviewsSettings : Grid, ISettingsPage
    {
        IModuleSettingsEditor _editor = null;
        Type _Selected;
        public Type Selected
        {
            get { return _Selected; }
            set
            {
                ContainerSettings.Children.Clear();
                try
                {
                    _editor = value?.SettingsEditor();
                    if (_editor != null)
                        ContainerSettings.Children.Add((UIElement)_editor);
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

        public PreviewsSettings()
        {
            InitializeComponent();
            Selected = Core.PreviewsContainerType;
            _IsEdited = false;
        }

        public bool Apply()
        {
            try
            {
                var settings = _editor?.Settings;
                var _tmp = (IPreviewsContainer)Activator.CreateInstance(_Selected, settings);
                Core.SaveSettings(settings);
                Core.PreviewsContainer = _tmp;
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
            Containers.SelectedItem = Core.PreviewsContainerType;
            _IsEdited = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
