using System;
using System.Threading;
using Booru.Core;

namespace Booru.Base
{
    public class PageTaskItem : BindableBase, ITaskItem
    {
        protected ConfiguredPair<IParserSettings> _Parser;
        protected ConfiguredPair<IPageGeneratorSettings> _Generator;
        protected string _Tags;
        protected Uri _Host;
        public int ServerID { get; private set; }
        public bool ManuallyCancelled { get; private set; } = false;
        public CancellationTokenSource CTS { get; private set; } = new CancellationTokenSource();
        CancellationToken ITaskItem.Token => CTS.Token;

        class DownloadTask : DataLoadTaskData<PageTaskItem>, IDataLoadTaskData
        {
            public Uri[] Uri => new Uri[] { Owner.Url };
            public int ServerID => Owner.ServerID;
            public CancellationToken Token => Owner.CTS.Token;
            public object Identifier => Owner;
            public DownloadTask(PageTaskItem owner) : base(owner) { }
        }

        bool ITaskItem.AvailiableTaskFor(int ServerID) => this.ServerID == ServerID;
        IDataLoadTaskData ITaskItem.RetrieveDataLoadTaskFor(int ServerID)
        {
            if (this.ServerID == ServerID)
               return new DownloadTask(this);
            return null;
        }

        public string Tags
        {
            get { return _Tags; }
            set
            {
                _Tags = value;
                NotifyPropertyChanged(nameof(Tags));
            }
        }
        public ConfiguredPair<IParserSettings> Parser
        {
            get { return _Parser; }
            set
            {
                _Parser = value;
                NotifyPropertyChanged(nameof(Parser));
            }
        }
        public ConfiguredPair<IPageGeneratorSettings> PageGenerator
        {
            get { return _Generator; }
            set
            {
                _Generator = value;
                NotifyPropertyChanged(nameof(PageGenerator));
            }
        }
        public Uri Url { get; private set; }

        public Uri Host
        {
            get { return _Host; }
            set
            {
                _Host = value;
                NotifyPropertyChanged(nameof(Host));
            }
        }

        public void Next(byte[] data)
        {
            Url = Core.Core.GetPageGenerator(PageGenerator.Type).GetNext(Host, Tags, data, PageGenerator.Settings);
            NotifyPropertyChanged(nameof(Url));
        }

        public PageTaskItem(Uri Host, ConfiguredPair<IParserSettings> Parser, ConfiguredPair<IPageGeneratorSettings> PageGenerator, string Tags)
        {
            this.Tags = Tags;
            this.Host = Host;
            ServerID = Core.Core.getServerID(Host);
            Url = Core.Core.GetPageGenerator(PageGenerator.Type).Init(Host, Tags, PageGenerator.Settings);
            this.Parser = Parser;
            this.PageGenerator = PageGenerator;
        }
    }
}
