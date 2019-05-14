using System;

namespace PhotoLabel.Services
{
    public interface IFolderWatcher
    {
        IDisposable Subscribe(IFolderObserver observer);
        void Dispose();
        void Watch(string path);
    }
}