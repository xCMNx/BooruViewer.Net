using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Booru.Core;

namespace Booru.Base
{
    public class TasksTemplate<T> : BindableBase, IDataLoaderSource where T : ITaskItem
    {
        protected ObservableCollection<T> _Tasks = new ObservableCollection<T>();
        protected List<T> _AvailiableTasks = new List<T>();
        public ObservableCollection<T> Tasks => _Tasks;

        public bool IsDataReady { get; private set; }

        public IDataLoadTaskData NextTaskData(int ServerId)
        {
            if (IsDataReady)
                lock (_Tasks)
                {
                    for (int i = 0; i < _AvailiableTasks.Count; i++)
                        if (!_AvailiableTasks[i].Token.IsCancellationRequested && _AvailiableTasks[i].AvailiableTaskFor(ServerId))
                        {
                            var t = _AvailiableTasks[i];
                            _AvailiableTasks.RemoveAt(i);
                            IsDataReady = _AvailiableTasks.Count > 0;
                            return t.RetrieveDataLoadTaskFor(ServerId);
                        }
                }
            return null;
        }

        public event Action<IDataLoaderSource> OnNextReady;

        protected void NotifyNextReady()
        {
            if (OnNextReady != null)
                OnNextReady(this);
        }

        protected void InternalAdd(T task)
        {
            lock (_Tasks)
            {
                _Tasks.Add(task);
                _AvailiableTasks.Add(task);
            }
            IsDataReady = true;
            NotifyNextReady();
        }

        protected virtual bool TaskComplited(IDataLoadTaskData Data, TaskResult result)
        {
            return true;
        }

        public void DataTaskComplited(IDataLoadTaskData Data, TaskResult result)
        {
            lock (_Tasks)
            {
                if (TaskComplited(Data, result))
                    _Tasks.Remove((T)Data.Identifier);
                else
                    _AvailiableTasks.Add((T)Data.Identifier);
                IsDataReady = _AvailiableTasks.Count > 0;
                NotifyNextReady();
            }
        }

    }
}
