using System;
using Booru.Core;

namespace Booru.Base
{
    public class PagesTasks : TasksTemplate<PageTaskItem>
    {
        protected override bool TaskComplited(IDataLoadTaskData Data, TaskResult result)
        {
            var t = (PageTaskItem)Data.Identifier;
            if (result == TaskResult.Completed)
            {
                if (!t.CTS.IsCancellationRequested)
                {
                    var ct = (BaseDataLoadTaskData)Data;
                    ct.Data.Position = 0;
                    var data = new byte[ct.Data.Length];
                    ct.Data.Read(data, 0, data.Length);
                    ct.Data = null;
                    var p = Core.Core.GetParser(t.Parser.Type);
                    int nd, nt;
                    Core.Core.DataContainer.AddDataRecords(p.GetData(data, t.Host.Host, t.Parser.Settings), out nd, out nt);
                    t.Next(data);
                    return t.Url == null;
                }
            }
            return true;
        }

        public void AddTask(Uri Host, ConfiguredPair<IParserSettings> Parser, ConfiguredPair<IPageGeneratorSettings> PageGenerator, string Tags)
        {
            InternalAdd(new PageTaskItem(Host, Parser, PageGenerator, Tags));
        }

    }
}
