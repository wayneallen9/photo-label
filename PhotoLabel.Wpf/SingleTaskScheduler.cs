using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoLabel.Wpf
{
    public class SingleTaskScheduler : TaskScheduler, IDisposable
    {
        #region variables

        private bool _disposedValue;
        private readonly BlockingCollection<Task> _tasks;
        #endregion

        public SingleTaskScheduler()
        {
            // initialise variables
            _tasks = new BlockingCollection<Task>();

            // create the thread that will process each task
            var taskThread = new Thread(TaskThread);
            taskThread.Start(_tasks);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks.ToArray();
        }

        protected override void QueueTask(Task task)
        {
            // a task must be specified
            if (task == null) throw new ArgumentNullException(nameof(task));
            try
            {
                // add this task to the queue
                _tasks.Add(task);
            }
            catch (InvalidOperationException)
            {
                // ignored
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        private void TaskThread(object state)
        {
            var tasks = (BlockingCollection<Task>) state;

            try
            {
                while (!tasks.IsAddingCompleted)
                {
                    // wait for a task to be added
                    var task = tasks.Take();

                    TryExecuteTask(task);

                    // don't overwhelm the UI
                    Thread.Sleep(50);
                }
            }
            catch (InvalidOperationException)
            {
                // ignored
            }
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                // flag that we are disposing
                _tasks.CompleteAdding();
            }

            _disposedValue = true;
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}