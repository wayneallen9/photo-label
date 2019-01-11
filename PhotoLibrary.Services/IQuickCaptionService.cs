using System;

namespace PhotoLabel.Services
{
    public interface IQuickCaptionService
    {
        void Add(Models.Metadata image);
        void Clear();
        IDisposable Subscribe(IQuickCaptionObserver observer);
        void Switch(Models.Metadata image);
    }
}