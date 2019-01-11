using System;
using System.Threading;

namespace PhotoLabel.Services
{
    public interface IRecentlyUsedDirectoriesService
    {
        void Load(CancellationToken cancellationToken);
        void Add(string directory);
        string GetMostRecentlyUsedDirectory();
        string GetMostRecentlyUsedFile();
        void SetLastSelectedFile(string filename);
        IDisposable Subscribe(IRecentlyUsedDirectoriesObserver observer);
    }
}