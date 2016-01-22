using Booru.Core;

namespace Booru.Base.DataLoadManager
{
    class DownloadTaskItem : IDownloadTask
    {
        protected IDataLoadTaskData _Data;
        protected IDataLoader _Loader;
        public IDataLoadTaskData Data => _Data;
        public IDataLoader Loader => _Loader;

        public DownloadTaskItem(IDataLoadTaskData Data, IDataLoader Loader)
        {
            _Data = Data;
            _Loader = Loader;
        }
    }
}
