using System;

namespace PhotoLabel.Wpf
{
    public interface IUiThrottler
    {
        void Queue(Action action);
    }
}