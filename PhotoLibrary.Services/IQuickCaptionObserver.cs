using System;

namespace PhotoLabel.Services
{
    public interface IQuickCaptionObserver
    {
        void OnClear();
        void OnCompleted();
        void OnError(Exception error);
        void OnNext(string caption);
    }
}