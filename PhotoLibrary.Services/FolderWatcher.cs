using System;
using System.Collections.Generic;
using System.IO;
using PhotoLabel.DependencyInjection;

namespace PhotoLabel.Services
{
    public class FolderWatcher : IDisposable, IFolderWatcher
    {
        public FolderWatcher()
        {
            // create dependencies
            _logService = NinjectKernel.Get<ILogService>();

            // initialise variables
            _fileSystemWatchers = new List<FileSystemWatcher>();
            _observers = new List<IFolderObserver>();
        }

        private FileSystemWatcher CreateFileSystemWatcher(string path)
        {
            var fileSystemWatcher = new FileSystemWatcher(path)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };

            fileSystemWatcher.Error += FileSystemWatcher_Error;
            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            fileSystemWatcher.Created += FileSystemWatcher_Created;
            fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;
            fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;

            return fileSystemWatcher;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                _fileSystemWatchers.ForEach(f => f.Dispose());
            }

            _disposedValue = true;
        }

        private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            // create dependencies
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace($@"Notifying {_observers.Count} observers of rename from ""{e.OldFullPath}"" to ""{e.FullPath}""...");
                foreach (var observer in _observers) observer.OnRenamed(e.OldFullPath, e.FullPath);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace($@"Notifying {_observers.Count} observers of new file {e.FullPath}...");
                foreach (var observer in _observers) observer.OnCreated(e.FullPath);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace($@"Notifying {_observers.Count} observers of new file {e.FullPath}...");
                foreach (var observer in _observers) observer.OnChanged(e.FullPath);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private void FileSystemWatcher_Error(object sender, ErrorEventArgs e)
        {
            // create dependencies
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace("Bubbling error up...");
                OnError(e.GetException());
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private void OnError(Exception ex)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Notifying {_observers.Count} observers of error...");
                foreach (var observer in _observers) observer.OnError(ex);
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public IDisposable Subscribe(IFolderObserver observer)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if observer is already subscribed...");
                if (_observers.Contains(observer))
                {
                    _logService.Trace("Observer is already subscribed.  Exiting...");
                    return new Unsubscriber<IFolderObserver>(_observers, observer);
                }

                _logService.Trace("Adding new observer...");
                _observers.Add(observer);

                return new Unsubscriber<IFolderObserver>(_observers, observer);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void Watch(string path)
        {
            // create dependencies
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                _logService.Trace($@"Watching for changes to ""{path}""...");
                _fileSystemWatchers.Add(CreateFileSystemWatcher(path));
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                logService.TraceExit();
            }
        }

        #region variables

        private bool _disposedValue; // To detect redundant calls
        private readonly List<FileSystemWatcher> _fileSystemWatchers;
        private readonly ILogService _logService;
        private readonly List<IFolderObserver> _observers;
        #endregion

        #region IDisposable
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}