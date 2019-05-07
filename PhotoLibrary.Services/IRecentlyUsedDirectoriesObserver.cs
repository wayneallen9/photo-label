using System;

namespace PhotoLabel.Services
{
    public interface IRecentlyUsedDirectoriesObserver
    {
        void OnClear();
        void OnError(Exception error);
        void OnNext(Models.Folder directory);
    }
}