using System;
using System.Collections.Generic;

namespace PhotoLabel.ViewModels
{
    internal class Unsubscriber : IDisposable
    {
        #region variables
        private readonly IObserver _observer;
        private readonly IList<IObserver> _observers;
        #endregion

        public Unsubscriber(
            IList<IObserver> observers,
            IObserver observer)
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