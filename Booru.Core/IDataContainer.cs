using System;
using System.Collections;
using System.Collections.Generic;

namespace Booru.Core
{
	public class DataSourceAddRecordsException : Exception { };

	public interface IContainerSettings : IModuleSettings { }
	public interface IDataContainer: IEditableModule
	{
		void AddDataRecords(IEnumerable<DataRecord> data, out int newData, out int NewTags);
		void Stop();
		void WaitCompletition();
		int MD5Count();
		int MD5HighId();
		BitArray Filter(string Tag, int Size);
		DataRecord getInfo(int MD5id);
		string getStatistics();
	}
}
