using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoLabel.Wpf
{
    public class LifoTaskScheduler : TaskScheduler
    {
        #region variables

        private readonly ConcurrentStack<Task> _tasks;
        #endregion

        public LifoTaskScheduler()
        {
            // initialise variables
            _tasks = new ConcurrentStack<Task>();

            // start the task processor on a background thread
            new Thread(TasksThread).Start(_tasks);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks.ToArray();
        }

        protected override void QueueTask(Task task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            // queue this task for execution
            _tasks.Push(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        private void TasksThread(object state)
        {
            var tasks = (ConcurrentStack<Task>) state;

            try
            {
                while (true)
                {
                    if (tasks.TryPop(out Task task))
                    {
                        TryExecuteTask(task);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // ignored
            }
        }
    }
}