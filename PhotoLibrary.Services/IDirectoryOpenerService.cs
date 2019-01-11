using System;
using System.Threading;

namespace PhotoLabel.Services
{
    public interface IDirectoryOpenerService
    {
        void Find(string directory, CancellationToken cancellationToken);
        IDisposable Subscribe(IDirectoryOpenerObserver observer);
    }
}