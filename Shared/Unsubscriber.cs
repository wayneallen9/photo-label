using System;
using System.Collections.Generic;

namespace Shared
{
    public class Unsubscriber<T> : IDisposable
    {
        public Unsubscriber(IList<T> observers, T observer)
        {
            // save parameters
            _observers = observers;
            _observer = observer;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                // remove the observer from the list
                _observers.Remove(_observer);
            }

            _disposedValue = true;
        }

        #region variables

        private bool _disposedValue; // To detect redundant calls
        private readonly IList<T> _observers;
        private readonly T _observer;

        #endregion

        #region IDisposable
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}