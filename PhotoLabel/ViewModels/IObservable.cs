using System;

namespace PhotoLabel.ViewModels
{
    public interface IObservable
    {
        IDisposable Subscribe(IObserver observer);
    }
}