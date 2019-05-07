using PhotoLabel.Services.Models;
using System;
using System.Threading;

namespace PhotoLabel.Services
{
    public interface IRecentlyUsedFoldersService
    {
        void Load(CancellationToken cancellationToken);
        void Add(Folder folder);
        Folder GetMostRecentlyUsedDirectory();
        string GetMostRecentlyUsedFile();
        void SetLastSelectedFile(string filename);
        IDisposable Subscribe(IRecentlyUsedDirectoriesObserver observer);
    }
}