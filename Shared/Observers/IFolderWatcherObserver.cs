using System;

namespace Shared.Observers
{
    public interface IFolderWatcherObserver
    {
        void OnCreated(string path);
        void OnError(Exception ex);
    }
}