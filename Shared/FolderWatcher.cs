using Shared.Observers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Shared
{
    public class FolderWatcher : IDisposable, IFolderWatcher
    {
        #region variables

        private bool _disposedValue;
        private readonly ILogger _logger;
        private readonly List<IFolderWatcherObserver> _observers;
        private readonly Dictionary<string, Watch> _watchers;

        #endregion

        public FolderWatcher(
            ILogger logger)
        {
            // save dependencies
            _logger = logger;

            // initialise variables
            _observers = new List<IFolderWatcherObserver>();
            _watchers = new Dictionary<string, Watch>();
        }

        public bool Add(string path)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($@"Watching ""{path}"" for all files...");
                return Add(path, ".");
            }
        }

        public bool Add(string path, string filterPattern)
        {
            using (var logger = _logger.Block())
            {
                if (string.IsNullOrWhiteSpace(filterPattern)) throw new ArgumentNullException(nameof(filterPattern));

                logger.Trace($@"Checking if the folder ""{path}"" exists...");
                if (!Directory.Exists(path))
                {
                    logger.Trace($@"""{path}"" does not exist.  Returning...");
                    return false;
                }

                logger.Trace($@"Checking if the folder ""{path}"" is already being watched...");
                if (_watchers.ContainsKey(path))
                {
                    logger.Trace($@"Stop watching ""{path}""...");
                    _watchers[path].Dispose();
                    _watchers.Remove(path);
                }

                logger.Trace("Creating file system watcher...");
                var watch = new Watch(path, filterPattern);
                watch.Created += Created;
                watch.Error += Error;

                logger.Trace($@"Watching ""{path}"" with filter pattern ""{filterPattern}""...");
                _watchers.Add(path, watch);

                return true;
            }
        }

        public void Clear()
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($"Clearing {_watchers.Count} file system watchers...");
                foreach (var watch in _watchers.Values)
                {
                    watch.Dispose();
                }
                _watchers.Clear();
            }
        }

        private void Created(object sender, FileSystemEventArgs e)
        {
            using (var logger = _logger.Block())
            {
                if (!(sender is Watch watch)) return;

                logger.Trace($@"Checking if ""{e.FullPath}"" matches filter...");
                if (!watch.Match(e.FullPath))
                {
                    logger.Trace($@"""{e.FullPath}"" does not match filter.  Exiting...");
                    return;
                }

                logger.Trace($@"Notifying {_observers.Count} observers of ""{e.FullPath}""...");
                _observers.ForEach(o => o.OnCreated(e.FullPath));
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                foreach (var watch in _watchers) watch.Value.Dispose();
            }

            _disposedValue = true;
        }

        private void Error(object sender, ErrorEventArgs e)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($"Notifying {_observers.Count} observers of error...");
                _observers.ForEach(o => o.OnError(e.GetException()));
            }
        }

        public IDisposable Subscribe(IFolderWatcherObserver observer)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Checking if observer is already subscribed...");
                if (_observers.Contains(observer))
                {
                    logger.Trace("Observer is already subscribed.  Returning...");
                    return new Unsubscriber<IFolderWatcherObserver>(_observers, observer);
                }

                logger.Trace("Adding new observer...");
                _observers.Add(observer);

                return new Unsubscriber<IFolderWatcherObserver>(_observers, observer);
            }
        }

        private class Watch : IDisposable
        {
            #region events

            public FileSystemEventHandler Created;
            public ErrorEventHandler Error;
            #endregion

            #region variables

            private bool _disposedValue;
            private readonly FileSystemWatcher _fileSystemWatcher;
            private readonly Regex _filterRegex;

            #endregion

            public Watch(
                string path,
                string pattern)
            {
                // initialise variables
                _fileSystemWatcher = new FileSystemWatcher(path)
                {
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = false
                };
                _fileSystemWatcher.Error += FileSystemWatcher_Error;
                _fileSystemWatcher.Created += FileSystemWatcher_Created;

                _filterRegex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

            private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
            {
                Created?.Invoke(this, e);
            }

            private void FileSystemWatcher_Error(object sender, ErrorEventArgs e)
            {
                // bubble it up
                Error?.Invoke(this, e);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (_disposedValue) return;

                if (disposing)
                {
                    _fileSystemWatcher.Dispose();
                }

                _disposedValue = true;
            }

            public bool Match(string path)
            {
                return _filterRegex.IsMatch(path);
            }

            #region IDisposable
            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
            }
            #endregion
        }

        #region IDisposable
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}