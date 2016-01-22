using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Booru.Core;
using Booru.Ui;

namespace BooruViewer.Settings
{
    public class LoaderSettingsViewModel : BindableBase
    {
        public ObservableCollection<IDataLoader> DataLoaders { get; private set; }
        public Type[] LoadersTypes => Core.LoadersTypes;

        UIElement _Editor;
        public UIElement Editor
        {
            get { return _Editor; }
            private set
            {
                _Editor = value;
                NotifyPropertyChanged(nameof(Editor));
            }
        }

        Type _SelectedLoaderType;
        public Type SelectedLoaderType
        {
            get { return _SelectedLoaderType; }
            set
            {
                _SelectedLoaderType = value;
                NotifyPropertyChanged(nameof(SelectedLoaderType));
            }
        }

        IDataLoader _SelectedLoader = null;
        public IDataLoader SelectedLoader
        {
            get
            {
                return _SelectedLoader;
            }
            set
            {
                _SelectedLoader = value;
                Editor = null;
                if (value != null)
                {

                    var editortype = Core.EditorTypeFor(SelectedLoader.GetType(), ControlType.Xaml, false);
                    if (editortype != null)
                        Editor = (UIElement)Activator.CreateInstance(editortype, SelectedLoader.CurrentSettings);
                }
                NotifyPropertiesChanged(nameof(SelectedLoader));
            }
        }

        public Command DeleteCommand { get; private set; }
        public Command ApplyCommand { get; private set; }
        public Command AddCommand { get; private set; }

        public LoaderSettingsViewModel(ObservableCollection<IDataLoader> DataLoaders)
        {
            this.DataLoaders = DataLoaders;
            DeleteCommand = new Command((p) => Delete((IDataLoader)p));
            ApplyCommand = new Command((p) => Apply(p));
            AddCommand = new Command(
                (p) => AddLoaderEditor(), 
                (p)=> _SelectedLoaderType != null
            );
        }

        public void Delete(IDataLoader loader) => DataLoaders.Remove(loader);

        public void AddLoaderEditor()
        {
            SelectedLoader = null;
            var editortype = Core.EditorTypeFor(_SelectedLoaderType, ControlType.Xaml, true);
            if (editortype == null)
                editortype = Core.EditorTypeFor(_SelectedLoaderType, ControlType.Xaml, false);
            if (editortype == null)
                Editor = null;
            else
                Editor = (UIElement)Activator.CreateInstance(editortype, null);
        }

        bool SettingsExists(IModuleSettings settings)
        {
            try
            {
                foreach (var ldr in DataLoaders)
                    if (settings.CompareTo(ldr.CurrentSettings) == 0)
                        return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            return false;
        }

        protected void AddLoader(IModuleSettings settings)
        {
            try
            {
                if (SettingsExists(settings)) return;
                var nldr = (IDataLoader)Activator.CreateInstance(_SelectedLoaderType, settings);
                DataLoaders.Add(nldr);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        public void Apply(object Settings)
        {
            var settings = Settings as ILoaderSettings;
            if (_SelectedLoader != null)
            {
                if (SettingsExists(settings))
                {
                    MessageBox.Show("Loader with this settings already exists!");
                    return;
                }
                _SelectedLoader.ApplySettings(settings);
                NotifyPropertiesChanged(nameof(DataLoaders));
            }
            else
            {
                if (Settings is IList<IModuleSettings>)
                    foreach (var set in Settings as IList<IModuleSettings>)
                        AddLoader(set);
                else
                    AddLoader(settings);
            }
        }
    }
}
