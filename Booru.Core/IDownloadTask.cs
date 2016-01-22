
namespace Booru.Core
{
	public interface IDownloadTask
	{
		IDataLoadTaskData Data { get; }
		IDataLoader Loader { get; }
	}
}
