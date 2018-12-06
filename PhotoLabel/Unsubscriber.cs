using System;
using System.Collections.Generic;

namespace PhotoLabel
{
    internal class Unsubscriber : IDisposable
    {
        #region variables
        private readonly ViewModels.IObserver _observer;
        private readonly IList<ViewModels.IObserver> _observers;
        #endregion

        public Unsubscriber(
            IList<ViewModels.IObserver> observers,
            ViewModels.IObserver observer)
        {
            // save the parameters
            _observer = observer;
            _observers = observers;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_observers.Contains(_observer))
                        _observers.Remove(_observer);
                }

                disposedValue = true;
            }
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