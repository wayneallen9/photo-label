using Shared.Observers;
using System;

namespace Shared
{
    public interface IFolderWatcher
    {
        bool Add(string path);
        bool Add(string path, string filterPattern);
        void Clear();
        IDisposable Subscribe(IFolderWatcherObserver observer);
    }
}