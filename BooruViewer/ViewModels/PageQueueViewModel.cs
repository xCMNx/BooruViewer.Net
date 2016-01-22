using Booru.Base;
using Booru.Core;
using Booru.Ui;
using BooruViewer.Settings.ViewModels;

namespace BooruViewer
{
    public class PageQueueViewModel : BindableBase
    {
        ServerTemplate _Selected;
        public ServerTemplate SelectedServer
        {
            get { return _Selected; }
            set
            {
                _Selected = value;
                _GeneratorModel = _Selected == null ? null : new xSettingsEditorVM<IPageGeneratorSettings>(Core.PageGeneratorsTypes, _Selected.PageGenerator);
                CommandAdd.IsEnabled = _Selected != null;
                NotifyPropertiesChanged(nameof(SelectedServer), nameof(GeneratorModel));
            }
        }

        xSettingsEditorVM<IPageGeneratorSettings> _GeneratorModel;
        public xSettingsEditorVM<IPageGeneratorSettings> GeneratorModel => _GeneratorModel;

        string _Tags;
        public string Tags
        {
            get { return _Tags; }
            set
            {
                _Tags = value;
                NotifyPropertyChanged(nameof(Tags));
            }
        }

        public Command CommandAdd { get; }

        public PageQueueViewModel()
        {
            CommandAdd = new Command((p) => Add((ConfiguredPair<IPageGeneratorSettings>)p), p => _Selected != null) { IsEnabled = false };
        }

        public void Add(ConfiguredPair<IPageGeneratorSettings> Settings)
        {
            StaticData.PagesTasks.AddTask(_Selected.Server, _Selected.Parser, Settings, _Tags);
        }
    }
}
