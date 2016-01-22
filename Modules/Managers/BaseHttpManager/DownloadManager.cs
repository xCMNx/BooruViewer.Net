using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Booru.Core;
using Booru.Core.Utils;

namespace Booru.Base.DataLoadManager
{
    class DownloadManager : IDataLoadManager
    {
        bool _IsTerminated = false;

        public IModuleSettings CurrentSettings => null;
        public bool IsTerminated => _IsTerminated;

        public ObservableCollection<IDataLoaderSource> _dataSources = new ObservableCollection<IDataLoaderSource>();

        protected ObservableCollection<IDownloadTask> _Tasks = new ObservableCollection<IDownloadTask>();
        public IEnumerable<IDownloadTask> Tasks => _Tasks;
        public void ApplySettings(IManagerSettings CurrentSettings) { }

        List<Tuple<IDataLoadTaskData, DataTaskComplited>> PrivateTasks = new List<Tuple<IDataLoadTaskData, DataTaskComplited>>();

        void IDataLoadManager.AddSource(IDataLoaderSource src)
        {
            lock (this)
            {
                if (!_dataSources.Contains(src))
                {
                    _dataSources.Add(src);
                    src.OnNextReady += DataSource_OnNextReady;
                    _ready.Set();
                }
            }
        }

        void DataSource_OnNextReady(IDataLoaderSource src)
        {
            _ready.Set();
        }

        void IDataLoadManager.RemoveSource(IDataLoaderSource src)
        {
            lock (this)
            {
                if (_dataSources.Contains(src))
                {
                    _dataSources.Remove(src);
                    src.OnNextReady -= DataSource_OnNextReady;
                }
            }
        }

        Thread mainTh;
        ManualResetEvent _ready = new ManualResetEvent(false);

        public DownloadManager(IManagerSettings Settings)
        {
            mainTh = new Thread(Execute);
            Core.Core.globalCTS.Token.Register(() => _ready.Set());
            mainTh.Start();
            Core.Core.DataLoaders.CollectionChanged += DataLoaders_CollectionChanged;
        }

        private void DataLoaders_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    _ready.Set();
                    break;
            }
        }

        SortedDictionary<int, List<IDataLoader>> FillFreeServers()
        {
            var res = new SortedDictionary<int, List<IDataLoader>>();
            lock (this)
            {
                var idList = Core.Core.ServersID;
                List<IDataLoader> lst;
                foreach (var ldr in Core.Core.DataLoaders)
                    foreach (var srvId in idList)
                        if (ldr.CheckStatusFor(srvId) == DataLoaderStatus.Ok)
                            if (res.TryGetValue(srvId, out lst))
                                lst.Add(ldr);
                            else
                                res[srvId] = new List<IDataLoader>() { ldr };
                return res;
            }
        }

        int FindBestServer(SortedDictionary<int, List<IDataLoader>> srvAv)
        {
            var pair = srvAv.ElementAt(0);
            for (int i = srvAv.Count - 1; i > 0; i--)
                if (pair.Value.Count > srvAv.Values.ElementAt(i).Count)
                    pair = srvAv.ElementAt(i);
            return pair.Key;
        }

        IDataLoader FindBestLoader(int srvId, List<IDataLoader> ldrs)
        {
            var ldr = ldrs[0];
            var cnt = ldr.ServerTasksCount(srvId);
            for (int i = ldrs.Count - 1; i > 0; i--)
            {
                var tcnt = ldrs[i].ServerTasksCount(srvId);
                if (tcnt < cnt && ldr.TasksCount < ldrs[i].TasksCount)
                {
                    ldr = ldrs[i];
                    cnt = tcnt;
                }
            }
            return ldr;
        }

        DataLoadingResult StartDownload(DataTaskComplited OnComplete, IDataLoader loader, IDataLoadTaskData data)
        {
            DataLoadingResult result = DataLoadingResult.LoaderErr;
            var ts = CancellationTokenSource.CreateLinkedTokenSource(Core.Core.globalCTS.Token, data.Token);
            foreach (var uri in data.Uri)
            {
                if (ts.IsCancellationRequested)
                    return DataLoadingResult.Cancelled;
                result = loader.LoadMethod(uri, ts.Token, data.OnProcessCallback, 1024);
                if (result == DataLoadingResult.Ok || result == DataLoadingResult.Suspended || result == DataLoadingResult.Cancelled)
                    break;
            }
            if (OnComplete != null && result != DataLoadingResult.Suspended && result != DataLoadingResult.LoaderErr && !Core.Core.globalCTS.IsCancellationRequested)
                OnComplete(data, result == DataLoadingResult.Ok ? TaskResult.Completed : result == DataLoadingResult.Cancelled ? TaskResult.Cancelled : TaskResult.Error);
            return result;
        }

        public Task<TaskResult> GetData(IDataLoadTaskData Data)
        {
            var result = TaskResult.Cancelled;
            ManualResetEvent cmpl = new ManualResetEvent(false);
            DataTaskComplited oncmpl = (d, r) =>
            {
                result = r;
                cmpl.Set();
            };
            var task = new Tuple<IDataLoadTaskData, DataTaskComplited>(Data, oncmpl);
            lock (PrivateTasks)
            {
                PrivateTasks.Add(task);
                Data.Token.Register(() =>
                {
                    lock (PrivateTasks)
                    {
                        cmpl.Set();
                        PrivateTasks.Remove(task);
                    }
                });
            }
            _ready.Set();
            return Task.Factory.StartNew(() =>
            {
                cmpl.WaitOne();
                return result;
            }, Data.Token);
        }

        void StartDownloadTask(DataTaskComplited OnComplete, IDataLoader loader, IDataLoadTaskData data)
        {
            var item = new DownloadTaskItem(data, loader);
            lock (_Tasks)
            {
                _Tasks.Add(item);
            }
            Task.Factory.StartNew(() =>
            {
                switch (StartDownload(OnComplete, loader, data))
                {
                    case DataLoadingResult.Suspended:
                    case DataLoadingResult.LoaderErr:
                        var task = new Tuple<IDataLoadTaskData, DataTaskComplited>(data, OnComplete);
                        lock (PrivateTasks)
                        {
                            PrivateTasks.Add(task);
                            _ready.Set();
                        }
                        break;
                }
                lock (_Tasks)
                {
                    _Tasks.Remove(item);
                }
            }, item.Data.Token);
        }

        bool TryRunTask(DataTaskComplited OnComplete, IDataLoadTaskData data, SortedDictionary<int, List<IDataLoader>> srvAv)
        {
            var sid = data.ServerID;
            List<IDataLoader> ldrs;
            if (srvAv.TryGetValue(sid, out ldrs))
            {
                var ldr = FindBestLoader(sid, ldrs);
                if (ldr != null)
                {
                    StartDownloadTask(OnComplete, ldr, data);
                    if(ldr.CheckStatusFor(sid) == DataLoaderStatus.Busy)
                        ldrs.Remove(ldr);
                    if (ldrs.Count == 0)
                    {
                        srvAv.Remove(sid);
                        return true;
                    }
                }
            }
            return false;
        }

        void RunTasks(SortedDictionary<int, List<IDataLoader>> srvAv)
        {
            lock (PrivateTasks)
            {
                for (int i = 0; i < PrivateTasks.Count; i++)
                {
                    var t = PrivateTasks[i];
                    if (TryRunTask(t.Item2, t.Item1, srvAv))
                        PrivateTasks.RemoveAt(i--);
                }
                if (srvAv.Count == 0 && PrivateTasks.Count > 0)
                {
                    foreach (var t in PrivateTasks)
                        t.Item2(t.Item1, TaskResult.Busy);
                    PrivateTasks.Clear();
                }
            }
            IList<IDataLoaderSource> sources;
            lock (this)
            {
                sources = _dataSources.ToArray();
            }
            while (srvAv.Count > 0 && WebHelper.CanStartNewDownloading() && !Core.Core.globalCTS.IsCancellationRequested)
            {
                var srvId = FindBestServer(srvAv);
                var noData = true;
                foreach (var src in sources)
                {
                    var data = src.NextTaskData(srvId);
                    if (data != null)
                    {
                        noData = false;
                        if (TryRunTask(src.DataTaskComplited, data, srvAv))
                            break;
                    }
                }
                if (noData)
                    srvAv.Remove(srvId);
            }
        }

        bool Hasready()
        {
            lock (this)
            {
                return PrivateTasks.Count > 0 || _dataSources.FirstOrDefault(s => s.IsDataReady) != null;
            }
        }

        void Execute()
        {
            _IsTerminated = false;
            while (!Core.Core.globalCTS.IsCancellationRequested)
            {
                _ready.Reset();
                if (Hasready() && WebHelper.CanStartNewDownloading())
                    RunTasks(FillFreeServers());
                _ready.WaitOne(1000);
            }
            _IsTerminated = true;
        }
    }
}
