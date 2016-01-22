using Booru.Base;
using Booru.Core;

namespace BooruViewer
{
    public static class StaticData
    {

        static DataViewModel _DataModel = new DataViewModel();
        public static DataViewModel DataViewModel => _DataModel;

        static MainViewModel _MainViewModel = new MainViewModel();
        public static MainViewModel Main => _MainViewModel;

        static PagesTasks _PagesTasks = new PagesTasks();
        public static PagesTasks PagesTasks => _PagesTasks;

        static PreviewTasks _PreviewTasks = new PreviewTasks();
        public static PreviewTasks PreviewTasks => _PreviewTasks;

        static StaticData()
        {
            AssignTaskContainers();
            Core.PropertyChanged += Core_PropertyChanged;
        }

        public static void AssignTaskContainers()
        {
            Core.DataLoadManager?.AddSource(_PreviewTasks);
            Core.DataLoadManager?.AddSource(_PagesTasks);
        }

        private static void Core_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Core.DataLoadManager))
                AssignTaskContainers();
        }
    }
}
