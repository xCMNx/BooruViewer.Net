using Booru.Core;
using Booru.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace Booru.Base.Loader
{
    public class ResponseTimeoutException : Exception { };

    [SettingsType(typeof(HttpDataLoaderSettings))]
    public class HttpDataLoader : IDataLoader
    {
        protected WebProxy Proxy;
        public static int MaxDownloadsPerDataLoader { get; private set; }
        public static int MaxDownloadsPerServer { get; private set; }

        protected HttpDataLoaderSettings _CurrentSettings = null;
        public IModuleSettings CurrentSettings => _CurrentSettings;

        public HttpDataLoader(HttpDataLoaderSettings Settings)
        {
            ApplySettings(Settings);
        }

        public HttpDataLoader()
        {
        }

        static HttpDataLoader()
        {
            MaxDownloadsPerDataLoader = int.Parse(Helpers.ReadFromConfig("MaxDownloadsPerDataLoader", "3", true));
            MaxDownloadsPerServer = int.Parse(Helpers.ReadFromConfig("MaxDownloadsPerServer", "3", true));
        }

        public void ApplySettings(ILoaderSettings Settings)
        {
            _CurrentSettings = (HttpDataLoaderSettings)Settings;
            if (Settings != null && !string.IsNullOrWhiteSpace(_CurrentSettings.Server))
                Proxy = new WebProxy(_CurrentSettings.Server);
            else
                Proxy = null;
        }

        public override string ToString()
        {
            return Proxy?.Address.AbsoluteUri ?? "localhost";
        }

        Dictionary<int, int> _SrvCounter = new Dictionary<int, int>();
        Dictionary<int, long> _SrvTicks = new Dictionary<int, long>();
        int _TasksCount = 0;

        int IDataLoader.TasksCount { get { return _TasksCount; } }

        public int ServerTasksCount(int ServerID)
        {
            lock (this)
            {
                int cnt;
                if (_SrvCounter.TryGetValue(ServerID, out cnt))
                    return cnt;
                return 0;
            }
        }

        public DataLoadingResult LoadMethod(Uri Uri, CancellationToken ct, LoadingProcessCallback ProcessCallback, int bufferSize)
        {
            int ServerID;
            lock (this)
            {
                _TasksCount++;
                ServerID = Core.Core.getServerID(Uri);
                _SrvCounter[ServerID] = ServerTasksCount(ServerID) + 1;
                _SrvTicks[ServerID] = Helpers.TickCount + WebHelper.RequestInterval;
            }
            bool timeout = false;
            long addTicks = WebHelper.RequestInterval;
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                try
                {
                    var req = WebHelper.SetRequestSettings(WebRequest.Create(Uri));
                    try
                    {
                        req.Proxy = Proxy;
                        cts.Token.Register(() =>
                        {
                            req.Abort();
                        });
                        HttpWebResponse httpResponse;
                        long t = Helpers.TickCount;
                        using (var c = new CancellationTokenSource(10000))
                        {
                            c.Token.Register(() =>
                            {
                                timeout = true;
                                req.Abort();
                            });
                            httpResponse = (HttpWebResponse)req.GetResponse();
                        }
                        try
                        {
                            Stream httpResponseStream = httpResponse.GetResponseStream();
                            ProcessCallback(null, httpResponse.ContentLength);

                            byte[] buffer = new byte[bufferSize];
                            int bytesRead = 0;

                            while ((bytesRead = httpResponseStream.Read(buffer, 0, bufferSize)) != 0)
                            {
                                if (cts.Token.IsCancellationRequested)
                                    return DataLoadingResult.Cancelled;
                                ProcessCallback(buffer, bytesRead);
                            }
                            return DataLoadingResult.Ok;
                        }
                        finally
                        {
                            httpResponse.Close();
                        }
                    }
                    catch (WebException e)
                    {
                        if (e.Status == WebExceptionStatus.RequestCanceled)
                            if (timeout)
                                return DataLoadingResult.LoaderErr;
                            else
                                return DataLoadingResult.Cancelled;
                        if (e.Status == WebExceptionStatus.ProtocolError)
                        {
                            var response = e.Response as HttpWebResponse;
                            if (response != null)
                            {
                                Console.WriteLine(response.StatusCode.ToString());
                                switch (response.StatusCode)
                                {
                                    case (HttpStatusCode)429:
                                    case HttpStatusCode.ServiceUnavailable: //danbooru API 503 Service Unavailable: Server cannot currently handle the request, try again later
                                    case (HttpStatusCode)421://danbooru API 421 User Throttled: User is throttled, try again later
                                    case HttpStatusCode.InternalServerError:
                                        addTicks = 600000;
                                        return DataLoadingResult.Suspended;
                                    case HttpStatusCode.BadGateway:
                                    case HttpStatusCode.Forbidden:
                                    case HttpStatusCode.Unauthorized:
                                        addTicks = 1200000;
                                        return DataLoadingResult.Suspended;
                                }
                            }
                        }
                        return DataLoadingResult.UrlErr;
                    }
                    catch (Exception e)
                    {
                        Helpers.ConsoleWrite(string.Format("\r\n{0} => {1} => {2}\r\n", req.Proxy != null ? ((WebProxy)req.Proxy).Address.Authority : string.Empty, Uri.AbsoluteUri, e.Message), ConsoleColor.DarkYellow);
                        return DataLoadingResult.LoaderErr;
                    }
                }
                finally
                {
                    lock (this)
                    {
                        _SrvTicks[ServerID] = Helpers.TickCount + addTicks;
                        _SrvCounter[ServerID] = ServerTasksCount(ServerID) - 1;
                        _TasksCount--;
                    }
                }
            }
        }

        public DataLoaderStatus CheckStatusFor(int ServerID)
        {
            lock (this)
            {
                long ticks;
                int streams;
                if (
                    (!_SrvTicks.TryGetValue(ServerID, out ticks) || ticks < Helpers.TickCount)
                    && (!_SrvCounter.TryGetValue(ServerID, out streams) || streams < MaxDownloadsPerServer)
                    && (Proxy == null || _TasksCount < MaxDownloadsPerDataLoader)
                    )
                    return DataLoaderStatus.Ok;
                return DataLoaderStatus.Busy;
            }
        }
    }
}
