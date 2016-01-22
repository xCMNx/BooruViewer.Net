using System;
using Booru.Core;
using System.Linq;

namespace Booru.Base
{
    public class PreviewTasks : TasksTemplate<PreviewTaskItem>
    {
        protected override bool TaskComplited(IDataLoadTaskData Data, TaskResult result)
        {
            var ct = (DataLoadTaskData<PreviewTaskItem>)Data;
            if (!ct.Owner.CTS.IsCancellationRequested)
            {
                if (result == TaskResult.Completed)
                {
                    ct.Data.Position = 0;
                    Core.Core.PreviewsContainer.setPreview(ct.Owner.Md5, ct.Data, string.Empty);
                    ct.Owner.OnFinish();
                    return true;
                }
                return ct.Owner.isDead();
            }
            return true;
        }

        public void CancelTask(string md5)
        {
            lock (_Tasks)
            {
                PreviewTaskItem pt = _Tasks.FirstOrDefault(t => t.Md5 == md5);
                if (pt != null)
                {
                    _Tasks.Remove(pt);
                    _AvailiableTasks.Remove(pt);
                }
            }
        }

        public void CancelAll()
        {
            lock (_Tasks)
            {
                _Tasks.Clear();
                _AvailiableTasks.Clear();
            }
        }

        public bool AddTask(string md5, DataServer[] servers, Action onFinished)
        {
            lock(_Tasks)
            {
                foreach (var t in _Tasks)
                    if (t.Md5 == md5)
                        return false;
                try
                {
                    InternalAdd(new PreviewTaskItem(md5, servers, onFinished));
                }
                catch (DeadPreviewTask)
                {
                    return false;
                }
            }
            return true;
        }

    }
}
