using System.Collections;
using System.Collections.Generic;
using Booru.Core;
using Booru.Core.Utils;
using Booru.Ui;

namespace BooruViewer
{
    public class DataViewModel : BindableBase
    {
        public CancellableCommand SearchCommand { get; private set; }

        public DataItem[] Data { get; private set; } = null;

        string _Statistics = string.Empty;
        public string Statistics => _Statistics;

        public DataViewModel()
        {
            SearchCommand = new CancellableCommand((p, t) => Search((string)p), _=> Core.DataContainer!=null);
        }

        public void Search(string SearchExpression)
        {
            StaticData.PreviewTasks.CancelAll();
            BitArray map = Filter.Execute(SearchExpression, Core.DataContainer.MD5HighId(), (t, s) => Core.DataContainer.Filter(t, s), ref _Statistics);
            NotifyPropertyChanged(nameof(Statistics));
            List<DataItem> data = new List<DataItem>(map.Length);
            for (int i = 1; i < map.Length; i++)
                if (map[i])
                    data.Add(new DataItem(i));
            Data = data.ToArray();
            NotifyPropertyChanged(nameof(Data));
        }
    }
}
