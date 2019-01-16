using System;

namespace PhotoLabel.Services
{
    public interface IQuickCaptionService
    {
        void Add(string filename, Models.Metadata image);
        void Clear();
        void Remove(string filename);
        IDisposable Subscribe(IQuickCaptionObserver observer);
        void Switch(string filename, Models.Metadata image);
    }
}