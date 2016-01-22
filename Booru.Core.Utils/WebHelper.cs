using System;
using System.Net;

namespace Booru.Core.Utils
{
	public static class WebHelper
	{
		public static string UserAgent { get; private set; }

		public static long RequestInterval { get; private set; }

		public static int MaxDownloads { get; private set; }

		static object lockObj = new object();
		static int _DownloadsCount = 0;
		public static int DownloadsCount
		{ 
			get
			{ 
				lock(lockObj)
 				{
					return _DownloadsCount; 
				}
			} 
			private set
			{
				lock (lockObj)
				{
					_DownloadsCount = value;
				}
			}
		}

		public static void StartDownloading()
		{
			lock (lockObj)
			{
				_DownloadsCount++;
			}
		}

		public static void FinishDownloading()
		{
			lock (lockObj)
			{
				_DownloadsCount--;
			}
		}

		public static bool CanStartNewDownloading()
		{
			lock (lockObj)
			{
				return _DownloadsCount < MaxDownloads;
			}
		}

		static WebHelper()
		{
			UserAgent = Helpers.ReadFromConfig("UserAgent", "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.90 Safari/537.36", true);
			RequestInterval = long.Parse(Helpers.ReadFromConfig("RequestInterval", "3", true)) * 1000;
			MaxDownloads = int.Parse(Helpers.ReadFromConfig("MaxDownloads", "3", true));
		}

		public static HttpWebRequest SetRequestSettings(WebRequest req)
		{
			var request = (HttpWebRequest)req;
			request.UserAgent = UserAgent;
			request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
			request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
			return request;
		}

		public static HttpWebRequest NewRequest(object proxy, Uri uri)
		{
			var r = SetRequestSettings(WebRequest.Create(uri));
			r.Proxy = proxy == null ? null : proxy is IWebProxy ? (IWebProxy)proxy : new WebProxy((string)proxy);
			return r;
		}
	}
}
