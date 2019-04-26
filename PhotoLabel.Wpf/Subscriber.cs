using System;
using System.Collections.Generic;

namespace PhotoLabel.Wpf
{
    public class Subscriber : IDisposable
    {
        public Subscriber(IList<IObserver> observers, IObserver observer)
        {
            // save dependencies
            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
            _observers = observers ?? throw new ArgumentNullException(nameof(observers));
        }

        #region variables

        private readonly IObserver _observer;
        private readonly IList<IObserver> _observers;
        #endregion

        #region IDisposable Support
        private bool _disposedValue ; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                // unsubscribe the associated observer
                _observers.Remove(_observer);
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