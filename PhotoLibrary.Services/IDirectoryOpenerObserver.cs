using System;

namespace PhotoLabel.Services
{
    public interface IDirectoryOpenerObserver
    {
        void OnOpening(string directory);
        void OnOpened(string directory, int count);
        void OnError(Exception ex);
        void OnImageFound(string directory, string filename, Models.Metadata file);
        void OnProgress(string directory, int current, int count);
    }
}