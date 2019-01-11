using System;

namespace PhotoLabel.Services
{
    public interface IDirectoryOpenerObserver
    {
        void OnOpening(string directory);
        void OnOpened(string directory, int count);
        void OnError(Exception ex);
        void OnImageFound(string directory, Models.Metadata file);
    }
}