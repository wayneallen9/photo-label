using System;
using System.Collections.Generic;

namespace PhotoLabel.Services
{
    internal class Unsubscriber<T> : IDisposable
    {
        private readonly IList<T> _observers;
        private readonly T _observer;

        internal Unsubscriber(IList<T> observers, T observer)
        {
            // save parameters
            _observers = observers;
            _observer = observer;
        }

        public void Dispose()
        {
            if (_observers.Contains(_observer)) _observers.Remove(_observer);
        }
    }
}