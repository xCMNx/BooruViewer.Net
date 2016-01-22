using System;
using System.Threading;

namespace Booru.Core
{
	public interface IDataLoadTaskData 
	{
		Uri[] Uri { get; }
		int ServerID { get; }
		object Identifier { get; }
		CancellationToken Token { get; }
		LoadingProcessCallback OnProcessCallback { get; }
	}
}
