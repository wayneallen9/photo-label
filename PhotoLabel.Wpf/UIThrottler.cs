using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Windows;

namespace PhotoLabel.Wpf
{
    public class UiThrottler : IDisposable, IUiThrottler
    {
        #region variables

        private readonly BlockingCollection<Action> _actions;
        private bool _disposedValue;

        #endregion

        public UiThrottler()
        {
            // initialise variables
            _actions = new BlockingCollection<Action>();

            // start the thread that will run the actions
            new Thread(DelegateThread).Start(_actions);
        }

        public void Queue(Action action)
        {
            _actions.Add(action);
        }

        private static void DelegateThread(object state)
        {
            var actions = (BlockingCollection<Action>) state;

            while (!actions.IsCompleted)
            {
                try
                {
                    // get the next queued action
                    var action = actions.Take();

                    // run this action on the UI thread
                    Application.Current?.Dispatcher.Invoke(action);

                    // pause so that the UI thread doesn't get overwhelmed
                    Thread.Sleep(100);
                }
                catch (InvalidOperationException)
                {
                    // ignored
                }
            }
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                // stop the blocking queue
                _actions.CompleteAdding();
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