using System;

namespace PhotoLabel.Services
{
    public interface IRecentlyUsedFoldersObserver
    {
        void OnClear();
        void OnError(Exception error);
        void OnNext(Models.Folder folder);
    }
}