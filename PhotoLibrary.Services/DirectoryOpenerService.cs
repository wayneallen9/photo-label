using System;
using System.Collections.Generic;
using System.Threading;

namespace PhotoLabel.Services
{
    public class DirectoryOpenerService : IDirectoryOpenerService
    {
        #region variables

        private string _directoryCache;
        private readonly IDictionary<string, Models.Metadata> _filesCache;
        private readonly IImageMetadataService _imageMetadataService;
        private readonly IImageService _imageService;
        private readonly ILogService _logService;
        private readonly IList<IDirectoryOpenerObserver> _observers;

        #endregion

        public DirectoryOpenerService(
            IImageMetadataService imageMetadataService,
            IImageService imageService,
            ILogService logService)
        {
            // save dependencies
            _imageMetadataService = imageMetadataService;
            _imageService = imageService;
            _logService = logService;

            // initialise variables
            _filesCache = new Dictionary<string, Models.Metadata>();
            _observers = new List<IDirectoryOpenerObserver>();
        }

        private void Opening(string directory, CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                _logService.Trace("Clearing current list of files...");
                _filesCache.Clear();

                if (cancellationToken.IsCancellationRequested) return;
                _logService.Trace($"Notifying {_observers.Count} observers that file list has been cleared...");
                foreach (var observer in _observers)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    observer.OnOpening(directory);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void Find(string directory, CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                _logService.Trace($@"Finding image files in ""{directory}"" and it's sub-directories...");
                _directoryCache = directory;

                if (cancellationToken.IsCancellationRequested) return;
                Opening(directory, cancellationToken);

                if (cancellationToken.IsCancellationRequested) return;
                _logService.Trace($@"Finding image files in ""{directory}"" and it's sub-directories...");
                var filenames = _imageService.Find(directory);

                _logService.Trace($"Creating metadata for {filenames.Count} files...");
                for (var i=0; i<filenames.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    _logService.Trace($"Notifying {_observers.Count} of progress {i + 1} of {filenames.Count}...");
                    foreach (var observer in _observers) observer.OnProgress(directory, i + 1, filenames.Count);

                    _logService.Trace($"Getting filename at position {i}...");
                    var filename = filenames[i];

                    _logService.Trace($@"Loading metadata for ""{filename}""...");
                    var file = _imageMetadataService.Load(filename);

                    if (cancellationToken.IsCancellationRequested) return;
                    _logService.Trace($@"Notifying {_observers.Count} observers about ""{filename}""...");
                    foreach (var observer in _observers)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        observer.OnImageFound(directory, filename, file);
                    }

                    if (cancellationToken.IsCancellationRequested) return;
                    _logService.Trace($@"Saving ""{filename}"" in list of found metadata...");
                    _filesCache.Add(filename, file);
                }

                if (cancellationToken.IsCancellationRequested) return;
                _logService.Trace($@"Notifying {_observers.Count} observers that find has completed...");
                foreach (var observer in _observers)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    observer.OnOpened(directory, filenames.Count);
                }
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested) return;
                _logService.Error(ex);

                if (cancellationToken.IsCancellationRequested) return;
                _logService.Trace($@"Notifying {_observers.Count} observers of error...");
                foreach (var observer in _observers)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    observer.OnError(ex);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public IDisposable Subscribe(IDirectoryOpenerObserver observer)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if observer is already subscribed...");
                if (_observers.Contains(observer))
                {
                    _logService.Trace("Observer is already subscribed.  Returning...");
                    return new Unsubscriber<IDirectoryOpenerObserver>(_observers, observer);
                }

                _logService.Trace("Observer is not already subscribed.  Subscribing...");
                _observers.Add(observer);

                _logService.Trace("Providing observer with existing directory models...");
                foreach (var dictionaryEntry in _filesCache) observer.OnImageFound(_directoryCache, dictionaryEntry.Key, dictionaryEntry.Value);

                return new Unsubscriber<IDirectoryOpenerObserver>(_observers, observer);
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}