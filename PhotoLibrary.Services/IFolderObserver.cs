using System;

namespace PhotoLabel.Services
{
    public interface IFolderObserver
    {
        void OnChanged(string path);
        void OnCreated(string path);
        void OnError(Exception ex);
        void OnRenamed(string oldPath, string newPath);
    }
}