using System.Threading;
using Booru.Core;

namespace Booru.Base
{
    public interface ITaskItem
    {
        IDataLoadTaskData RetrieveDataLoadTaskFor(int ServerID);
        bool AvailiableTaskFor(int ServerID);
        CancellationToken Token { get; }
    }
}
