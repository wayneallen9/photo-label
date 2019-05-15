using System;

namespace Shared.Observers
{
    public interface IFolderWatcherObserver
    {
        void OnChanged(string path);
        void OnCreated(string path);
        void OnDeleted(string path);
        void OnError(Exception ex);
        void OnRenamed(string oldPath, string newPath);
    }
}