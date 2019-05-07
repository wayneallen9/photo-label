using PhotoLabel.Services.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace PhotoLabel.Services
{
    public class RecentlyUsedDirectoriesService : IRecentlyUsedFoldersService
    {
        #region delegates

        #endregion

        #region variables

        private readonly ILogService _logService;
        private readonly List<IRecentlyUsedDirectoriesObserver> _observers;
        private readonly List<Folder> _recentlyUsedDirectories;
        private readonly IXmlFileSerialiser _xmlFileSerialiser;

        #endregion

        public RecentlyUsedDirectoriesService(
            ILogService logService,
            IXmlFileSerialiser xmlFileSerialiser)
        {
            // save the dependency injections
            _logService = logService;
            _xmlFileSerialiser = xmlFileSerialiser;

            // initialise variables
            _observers = new List<IRecentlyUsedDirectoriesObserver>();
            _recentlyUsedDirectories = new List<Folder>();
        }

        private void Clear()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Clearing current list of recently used files...");
                _recentlyUsedDirectories.Clear();

                _logService.Trace($"Notifiying {_observers.Count} observers that list has been cleared...");
                foreach (var observer in _observers) observer.OnClear();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void Load(CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                Clear();

                if (cancellationToken.IsCancellationRequested) return;
                _logService.Trace("Getting path to recently used directories file...");
                var filename = GetFilename();

                if (cancellationToken.IsCancellationRequested) return;
                _logService.Trace($@"Loading recently used directories from ""{filename}""...");
                _recentlyUsedDirectories.AddRange(_xmlFileSerialiser.Deserialise<List<Folder>>(filename) ??
                                                  new List<Folder>());

                foreach (var observer in _observers)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    SendRecentlyUsedDirectories(observer);
                }
            }
            catch (Exception ex)
            {
                _logService.Trace($"Notifying {_observers.Count} observers of error...");
                foreach (var observer in _observers)
                {
                    observer.OnError(ex);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private string GetFilename()
        {
            _logService.TraceEnter();
            try
            {
                // build the filename for the recently used files
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Photo Label",
                    "Recently Used Files.xml");
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void Add(Folder folder)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Adding ""{folder.Path}"" to the list of recently used directories...");

                _logService.Trace($@"Checking if ""{folder.Path}"" is already in the list...");
                var entry = _recentlyUsedDirectories.FirstOrDefault(d => d.Path == folder.Path);
                if (entry == null)
                {
                    _logService.Trace($@"""{folder.Path}"" is not in the list.  Adding it...");
                    _recentlyUsedDirectories.Insert(0, folder);

                    _logService.Trace($@"Notifying {_observers.Count} of ""{folder.Path}""...");
                    foreach (var observer in _observers) SendRecentlyUsedDirectories(observer);

                    // save the list
                    Save();
                }
                else
                {
                    _logService.Trace($@"Removing ""{folder.Path}"" from list...");
                    _recentlyUsedDirectories.Remove(entry);

                    _logService.Trace($@"Inserting ""{folder.Path}"" at the top of the list...");
                    _recentlyUsedDirectories.Insert(0, folder);

                    _logService.Trace($@"Notifying {_observers.Count} of ""{folder.Path}""...");
                    foreach (var observer in _observers) SendRecentlyUsedDirectories(observer);

                    // save the list
                    Save();
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Folder GetMostRecentlyUsedDirectory()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Returning path to the most recently used directory...");
                return _recentlyUsedDirectories.FirstOrDefault();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public string GetMostRecentlyUsedFile()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Returning path to the most recently used file...");
                return _recentlyUsedDirectories.FirstOrDefault()?.Filename;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void Save()
        {
            _logService.TraceEnter();
            try
            {
                // save the change
                var filename = GetFilename();

                _logService.Trace($@"Saving recently used directories to ""{filename}""...");
                _xmlFileSerialiser.Serialise(_recentlyUsedDirectories, filename);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void SetLastSelectedFile(string filename)
        {
            var stopWatch = Stopwatch.StartNew();

            _logService.TraceEnter();
            try
            {
                _logService.Trace("Getting most recently used directory...");
                var mostRecentlyUsedDirectory = _recentlyUsedDirectories.First();

                _logService.Trace(
                    $@"Setting most recently used file for ""{mostRecentlyUsedDirectory.Path}"" to ""filename""...");
                mostRecentlyUsedDirectory.Filename = filename;

                Save();
            }
            finally
            {
                _logService.TraceExit(stopWatch);
            }
        }

        public IDisposable Subscribe(IRecentlyUsedDirectoriesObserver observer)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if observer is already subscribed...");
                if (_observers.Contains(observer))
                    return new Unsubscriber<IRecentlyUsedDirectoriesObserver>(_observers, observer);

                _logService.Trace("Observer is not subscribed.  Subscribing...");
                _observers.Add(observer);

                SendRecentlyUsedDirectories(observer);

                return new Unsubscriber<IRecentlyUsedDirectoriesObserver>(_observers, observer);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void SendRecentlyUsedDirectories(IRecentlyUsedDirectoriesObserver observer)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Notifying observer to clear list...");
                observer.OnClear();

                _logService.Trace("Notifying observer of current state...");
                foreach (var directory in _recentlyUsedDirectories)
                {
                    _logService.Trace($@"Notifying observer of ""{directory}""...");
                    observer.OnNext(directory);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}