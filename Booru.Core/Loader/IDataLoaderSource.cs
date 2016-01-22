using System;

namespace Booru.Core
{
	public interface IDataLoaderSource
	{
		IDataLoadTaskData NextTaskData(int ServerId);
		event Action<IDataLoaderSource> OnNextReady;
		bool IsDataReady { get; }
		void DataTaskComplited(IDataLoadTaskData Data, TaskResult Succes);
	}
}
