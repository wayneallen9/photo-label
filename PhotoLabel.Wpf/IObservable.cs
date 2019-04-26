using System;

namespace PhotoLabel.Wpf
{
    public interface IObservable
    {
        IDisposable Subscribe(IObserver observer);
    }
}