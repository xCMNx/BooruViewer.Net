using System;
using Booru.Core;

namespace BooruViewer.Settings.ViewModels
{
    public class xSettingsEditorVM<T> : BindableBase where T : class,IModuleSettings
    {
        Type _SelectedType;
        public Type SelectedType
        {
            get { return _SelectedType; }
            set
            {
                _SelectedType = value;
                Editor = GetEditor(value);
                NotifyPropertyChanged(nameof(SelectedType));
            }
        }

        IModuleSettingsEditor _Editor;
        public IModuleSettingsEditor Editor
        {
            get { return _Editor; }
            set
            {
                Booru.Ui.UiHelper.ChangeObject(ref _Editor, Route, value);
                NotifyPropertyChanged(nameof(Editor));
            }
        }

        public ConfiguredPair<T> Settings => _SelectedType == null ? null : new ConfiguredPair<T>(_SelectedType, (T)_Editor?.Settings.Clone());

        ConfiguredPair<T> _StartSettings;

        public Type[] TypeList { get; private set; }

        public xSettingsEditorVM(Type[] typeList, ConfiguredPair<T> Settings = null)
        {
            TypeList = typeList;
            _StartSettings = Settings;
            SelectedType = _StartSettings?.Type;
        }

        IModuleSettingsEditor GetEditor(Type type)
        {
            if (type != null)
            {
                var eType = Core.EditorTypeFor(type, ControlType.Xaml);
                if (eType != null)
                    return (IModuleSettingsEditor)Activator.CreateInstance(eType, _StartSettings?.Type == type ? _StartSettings.Settings?.Clone() : null);
            }
            return null;
        }

        public bool IsConfigured
        {
            get
            {
                if (_SelectedType == null) return false;
                T gs = (T)_Editor?.Settings;
                if ((gs == null || !gs.isConfigured) && Core.IsSettingsRequiered(_SelectedType))
                    return false;
                return true;
            }
        }

        public void CheckIsConfigured()
        {
            if (_SelectedType == null)
                throw new Exception("Type is not selected.");
            T gs = (T)_Editor?.Settings;
            if ((gs == null || !gs.isConfigured) && Core.IsSettingsRequiered(_SelectedType))
                throw new Exception("Type is not configured.");
        }
    }
}
