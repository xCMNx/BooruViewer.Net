using System.IO;
using Booru.Core;

namespace Booru.Base
{
    public class BaseDataLoadTaskData
    {
        public MemoryStream Data = new MemoryStream();
        public LoadingProcessCallback OnProcessCallback => (byte[] data, long size) =>
        {
            if (data != null)
                Data.Write(data, 0, (int)size);
        };
    }

    public class DataLoadTaskData<T> : BaseDataLoadTaskData
    {
        public readonly T Owner;
        public DataLoadTaskData(T owner)
        {
            Owner = owner;
        }
    }
}
