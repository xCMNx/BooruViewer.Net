using System;
using System.Threading;

namespace Booru.Core
{
    public delegate void LoadingProcessCallback(byte[] data, long size);
    public enum DataLoaderStatus { Busy, Ok };
    public enum DataLoadingResult
    {
        UrlErr,     //Url is invalid
        LoaderErr,  //Some trouble with loader, try other loader
        Suspended,  //Timeout/InternalError or something else, url maybe OK, but connection failed. Try other loader
        Cancelled,
        Ok          //Successfully completed
    }
    public interface ILoaderSettings : IModuleSettings { }
    public interface IDataLoader : IEditableModule
    {
        DataLoadingResult LoadMethod(Uri Uri, CancellationToken ct, LoadingProcessCallback ProcessCallback, int bufferSize);
        void ApplySettings(ILoaderSettings Settings);
        DataLoaderStatus CheckStatusFor(int ServerID);
        int TasksCount { get; }
        int ServerTasksCount(int ServerID);
    }
}
