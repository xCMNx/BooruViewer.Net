using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Booru.Base;
using Booru.Core;
using Booru.Ui;
using BooruViewer.Settings.ViewModels;

namespace BooruViewer.Settings
{
    public class ServerSettingsViewModel : BindableBase, ISettingsPage
    {
        public bool CanClose => !_IsEdited;
        protected bool _IsEdited = false;
        public bool IsEdited
        {
            get
            {
                return _IsEdited;
            }
            set
            {
                var old = _IsEdited;
                _IsEdited = value;
                if (old != _IsEdited)
                {
                    NotifyPropertyChanged(nameof(IsEdited));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        xSettingsEditorVM<IPageGeneratorSettings> _GeneratorModel = new xSettingsEditorVM<IPageGeneratorSettings>(Core.PageGeneratorsTypes, null);
        public xSettingsEditorVM<IPageGeneratorSettings> GeneratorModel => _GeneratorModel;

        xSettingsEditorVM<IParserSettings> _ParserModel =  new xSettingsEditorVM<IParserSettings>(Core.ParsersTypes, null);
        public xSettingsEditorVM<IParserSettings> ParserModel => _ParserModel;

        string _FileMask;
        public string FileMask
        {
            get { return _FileMask; }
            set
            {
                _FileMask = value;
                IsEdited = true;
                NotifyPropertyChanged(nameof(FileMask));
            }
        }

        string _PreviewMask;
        public string PreviewMask
        {
            get { return _PreviewMask; }
            set
            {
                _PreviewMask = value;
                IsEdited = true;
                NotifyPropertyChanged(nameof(PreviewMask));
            }
        }

        string _Url;
        public string Url
        {
            get { return _Url; }
            set
            {
                _Url = value;
                IsEdited = true;
                CheckCommandTest();
                NotifyPropertyChanged(nameof(Url));
            }
        }

        ServerTemplate _SelectedSettings;
        public ServerTemplate SelectedSettings
        {
            get { return _SelectedSettings; }
            set
            {
                _SelectedSettings = value;

                Url = _SelectedSettings?.Server.AbsoluteUri;
                PreviewMask = _SelectedSettings?.PreviewMask;
                FileMask = _SelectedSettings?.FileMask;
                Booru.Ui.UiHelper.ChangeObject(ref _ParserModel, Gen_PropertyChanged, new xSettingsEditorVM<IParserSettings>(Core.ParsersTypes, _SelectedSettings?.Parser));
                Booru.Ui.UiHelper.ChangeObject(ref _GeneratorModel, Gen_PropertyChanged, new xSettingsEditorVM<IPageGeneratorSettings>(Core.PageGeneratorsTypes, _SelectedSettings?.PageGenerator));
                CheckCommandTest();

                IsEdited = false;
                DeleteCommand.IsEnabled = _SelectedSettings != null;
                NotifyPropertiesChanged(nameof(SelectedSettings), nameof(GeneratorModel), nameof(ParserModel));
            }
        }

        private void Gen_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CheckCommandTest();
            IsEdited = true;
        }

        void SettingsToTemplate(ServerTemplate tmpl)
        {
            _GeneratorModel.CheckIsConfigured();
            _ParserModel.CheckIsConfigured();
            tmpl.Parser = _ParserModel.Settings;
            tmpl.PageGenerator = _GeneratorModel.Settings;
            tmpl.FileMask = FileMask;
            tmpl.PreviewMask = PreviewMask;
        }

        public bool Apply()
        {
            try
            {
                if (_SelectedSettings != null)
                {
                    if (MessageBox.Show("Do you really want to replace setiings for this server?", "???", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        if (MessageBox.Show("Maybe you want to add a new server?", "???", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            _SelectedSettings = null;
                            NotifyPropertyChanged(nameof(SelectedSettings));
                        }
                        return false;
                    }
                    SettingsToTemplate(_SelectedSettings);
                    Templates.Save();
                    IsEdited = false;
                    return true;
                }
                var tmpl = new ServerTemplate(new Uri(Url));
                SettingsToTemplate(tmpl);
                Templates.Add(tmpl);
                SelectedSettings = tmpl;
                IsEdited = false;
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            return false;
        }

        public void Reset()
        {
            SelectedSettings = null;
        }

        public void New()
        {
            SelectedSettings = null;
        }

        public void Delete()
        {
            if (_SelectedSettings != null && MessageBox.Show($"Do you really want to delete template for {_SelectedSettings.Server.AbsoluteUri}?", "???", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                Templates.Remove(_SelectedSettings);
        }

        public Command DeleteCommand { get; private set; }
        public Command NewCommand { get; private set; }
        public CancellableCommand CommandTest { get; private set; }
        public Command CommandTestCancel { get; private set; }

        public ServerSettingsViewModel()
        {
            DeleteCommand = new Command(_ => Delete(), _ => _SelectedSettings != null);
            NewCommand = new Command(_ => New());
            CommandTest = new CancellableCommand((p, t) => Test(t), p => !string.IsNullOrWhiteSpace(_Url) && _GeneratorModel != null && _GeneratorModel.IsConfigured);
            CommandTestCancel = new Command(_ => CommandTest.Cancell());
            CommandTest.IsEnabled = false;
        }

        void CheckCommandTest()
        {
            CommandTest.IsEnabled = !string.IsNullOrWhiteSpace(_Url) && _GeneratorModel != null && _GeneratorModel.IsConfigured;
        }

        class dti : IDataLoadTaskData
        {
            Uri[] _Uri;
            int _ServerID;
            public Uri[] Uri => _Uri;
            public int ServerID => _ServerID;
            public object Identifier => null;
            public CancellationToken Token { get; private set; }
            public MemoryStream Data = new MemoryStream();
            public LoadingProcessCallback OnProcessCallback => (byte[] data, long size) =>
            {
                if (data != null)
                    Data.Write(data, 0, (int)size);
            };
            public dti(Uri uri, CancellationToken token)
            {
                Token = token;
                _Uri = new[] { uri };
                _ServerID = Core.getServerID(uri);
            }
        }

        public void Test(CancellationToken token)
        {
            try
            {
                var uri = new Uri(Url);
                if (Core.DataLoadManager == null)
                    throw new Exception("Data load manager isn't configured.");
                Core.MainContextSend(() => _GeneratorModel.CheckIsConfigured());
                var pg = Core.MainContextGet(()=>_GeneratorModel.Settings);
                var gen = Core.GetPageGenerator(pg.Type);
                if (gen != null)
                {
                    var pUri = gen.Init(uri, string.Empty, pg.Settings);
                    var taskData = new dti(pUri, token);
                    Task<TaskResult> t;
                    do
                    {
                        t = Core.DataLoadManager.GetData(taskData);
                        t.Wait();
                    } while (t.Result == TaskResult.Busy); 
                    if (t.Result == TaskResult.Completed)
                    {
                        var ps = Core.MainContextGet(() => _ParserModel.Settings);
                        if (ps.Type == null)
                            MessageBox.Show("OK");
                        else
                        {
                            var parser = Core.GetParser(ps.Type);
                            var data = parser.GetData(taskData.Data.ToArray(), _Url, ps.Settings);
                            Core.MainContextSend(()=>MessageBox.Show(string.Join("\r\n", data?.Select(d => $"{d.MD5}: {d.Rating} {string.Join(" ", d.Tags)}").Take(5).ToArray()), "Parser result"));
                        }
                    }
                    else
                        Core.MainContextSend(() => MessageBox.Show($"Failed to load ({t.Result}) {pUri.AbsoluteUri}"));
                }
            }
            catch (Exception ex)
            {
                Core.MainContextSend(()=>MessageBox.Show(ex.Message));
            }
        }
    }
}
