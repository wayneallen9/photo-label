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
                watch.Changed += Changed;
                watch.Created += Created;
                watch.Deleted += Deleted;
                watch.Error += Error;
                watch.Renamed += Renamed;

                logger.Trace($@"Watching ""{path}"" with filter pattern ""{filterPattern}""...");
                _watchers.Add(path, watch);

                return true;
            }
        }

        private void Renamed(object sender, RenamedEventArgs e)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($@"Notifying {_observers.Count} observers that ""{e.OldFullPath}"" has been renamed to ""{e.FullPath}""...");
                _observers.ForEach(o => o.OnRenamed(e.OldFullPath, e.FullPath));
            }
        }

        private void Changed(object sender, FileSystemEventArgs e)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($@"Notifying {_observers.Count} observers of ""{e.FullPath}""...");
                _observers.ForEach(o => o.OnChanged(e.FullPath));
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
                logger.Trace($@"Notifying {_observers.Count} observers of ""{e.FullPath}""...");
                _observers.ForEach(o => o.OnCreated(e.FullPath));
            }
        }

        private void Deleted(object sender, FileSystemEventArgs e)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($@"Notifying {_observers.Count} observers that ""{e.FullPath}"" has been deleted...");
                _observers.ForEach(o => o.OnDeleted(e.FullPath));
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                // dispose of all of the watchers
                foreach (var watch in _watchers) watch.Value.Dispose();

                // clear the list of watchers
                _watchers.Clear();
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

        #region IDisposable
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

        private sealed class Watch : IDisposable
        {
            #region events

            public FileSystemEventHandler Changed;
            public FileSystemEventHandler Created;
            public FileSystemEventHandler Deleted;
            public ErrorEventHandler Error;
            public RenamedEventHandler Renamed;
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
                _fileSystemWatcher.Changed += FileSystemWatcher_Changed;
                _fileSystemWatcher.Error += FileSystemWatcher_Error;
                _fileSystemWatcher.Created += FileSystemWatcher_Created;
                _fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;
                _fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;
                _filterRegex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

            private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
            {
                if (_filterRegex.IsMatch(e.OldFullPath))
                    Renamed?.Invoke(this, e);
            }

            private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
            {
                if (_filterRegex.IsMatch(e.FullPath))
                    Changed?.Invoke(this, e);
            }

            private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
            {
                if (_filterRegex.IsMatch(e.FullPath))
                    Deleted?.Invoke(this, e);
            }

            private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
            {
                if (_filterRegex.IsMatch(e.FullPath))
                    Created?.Invoke(this, e);
            }

            private void FileSystemWatcher_Error(object sender, ErrorEventArgs e)
            {
                // bubble it up
                Error?.Invoke(this, e);
            }

            private void Dispose(bool disposing)
            {
                if (_disposedValue) return;

                if (disposing)
                {
                    _fileSystemWatcher.Dispose();
                }

                _disposedValue = true;
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
}