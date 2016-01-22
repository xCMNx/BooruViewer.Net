using System;
using System.Threading;
using Booru.Core;
using System.Linq;

namespace Booru.Base
{
    public class DeadPreviewTask : Exception { } 
    public class PreviewTaskItem : BindableBase, ITaskItem
    {
        public readonly string Md5;
        public readonly DataServer[] Servers;
        public readonly Action OnFinish;
        int[] ServersId;
        public CancellationTokenSource CTS { get; private set; } = new CancellationTokenSource();

        CancellationToken ITaskItem.Token => CTS.Token;

        class DownloadTask : DataLoadTaskData<PreviewTaskItem>, IDataLoadTaskData
        {
            public Uri[] Uri { get; private set; }
            public int ServerID { get; private set; }
            public CancellationToken Token => Owner.CTS.Token;
            public object Identifier => Owner;
            public DownloadTask(PreviewTaskItem owner, int serverId, Uri[] uri) : base(owner)
            {
                ServerID = serverId;
                Uri = uri;
            }
        }

        public bool isDead()
        {
            foreach (var i in ServersId)
                if (i >= 0)
                    return false;
            return true;
        }

        bool ITaskItem.AvailiableTaskFor(int ServerID) => ServersId.Contains(ServerID);
        IDataLoadTaskData ITaskItem.RetrieveDataLoadTaskFor(int ServerID)
        {
            int i = 0;
            while (i < ServersId.Length && ServersId[i] != ServerID) i++;
            if (i < ServersId.Length)
            {
                ServersId[i] = -1;
                var ds = Servers[i];
                var tmpl = Templates.getTemplate(ds.Server);
                var urls = DataHelpers.GenerateUrls(tmpl.PreviewMask, ds, Md5, DataHelpers.PREV_EXT_LIST);
                if (urls != null)
                {
                    var uris = urls.Select(url => new Uri(url)).ToArray();
                    return new DownloadTask(this, ServerID, uris);
                }
            }
            return null;
        }

        public PreviewTaskItem(string md5, DataServer[] servers, Action onFinish)
        {
            Md5 = md5;
            Servers = servers;
            OnFinish = onFinish;
            ServersId = servers.Select(s => Core.Core.getServerID(s.Server)).ToArray();
            if (isDead())
                throw new DeadPreviewTask();
        }
    }
}
