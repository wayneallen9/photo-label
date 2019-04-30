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
        private readonly ConcurrentStack<Task> _tasks;
        private readonly Thread _taskThread;
        #endregion

        public SingleTaskScheduler()
        {
            // initialise variables
            _tasks = new ConcurrentStack<Task>();

            // create the thread that will process each task
            _taskThread = new Thread(TaskThread)
            {
                Priority = ThreadPriority.BelowNormal
            };
            _taskThread.Start(_tasks);
        }

        public void Clear()
        {
            _tasks.Clear();
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
                _tasks.Push(task);
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
            var tasks = (ConcurrentStack<Task>) state;

            try
            {
                while (true)
                {
                    // wait for a task to be added
                    if (!tasks.TryPop(out var task)) continue;

                    TryExecuteTask(task);
                }
            }
            catch (ThreadAbortException)
            {
                // ignored
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                // stop the background thread
                _taskThread.Abort();
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