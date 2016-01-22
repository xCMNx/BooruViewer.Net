
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Booru.Core
{
    public enum TaskResult { Completed, Busy, Error, Cancelled }
    public delegate void DataTaskComplited(IDataLoadTaskData Data, TaskResult Result);
    public interface IManagerSettings : IModuleSettings { }
    public interface IDataLoadManager : IEditableModule
    {
        void AddSource(IDataLoaderSource src);
        void RemoveSource(IDataLoaderSource src);
        void ApplySettings(IManagerSettings Settings);
        Task<TaskResult> GetData(IDataLoadTaskData Data);
        bool IsTerminated { get; }
        IEnumerable<IDownloadTask> Tasks { get; }
    }
}
