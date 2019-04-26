using System;

namespace PhotoLabel.Wpf
{
    public interface IObserver
    {
        void OnError(Exception ex);
    }
}