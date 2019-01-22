using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using PhotoLabel.Extensions;

namespace PhotoLabel.Services
{
    public class RecentlyUsedDirectoriesService : IRecentlyUsedDirectoriesService
    {
        #region delegates

        #endregion

        #region variables

        private readonly ILogService _logService;
        private readonly List<IRecentlyUsedDirectoriesObserver> _observers;
        private readonly List<Models.Directory> _recentlyUsedDirectories;
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
            _recentlyUsedDirectories = new List<Models.Directory>();
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

        private string GetCaption(string folder)
        {
            _logService.TraceEnter();
            try
            {
                // was a directory provided?
                if (string.IsNullOrWhiteSpace(folder)) return string.Empty;

                // is it less than 20 characters?
                if (folder.Length <= 20) return folder;

                // build it back up
                var root = folder.Substring(0, folder.IndexOf(Path.DirectorySeparatorChar, 2) + 1);
                var branch = folder.Substring(folder.LastIndexOf(Path.DirectorySeparatorChar));

                return $"{root}...{branch}";
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
                _recentlyUsedDirectories.AddRange(_xmlFileSerialiser.Deserialise<List<Models.Directory>>(filename) ??
                                                  new List<Models.Directory>());

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
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Photo Label",
                    "Recently Used Files.xml");
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void Add(string directory)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Adding ""{directory}"" to the list of recently used directories...");

                _logService.Trace($@"Checking if ""{directory}"" is already in the list...");
                var entry = _recentlyUsedDirectories.FirstOrDefault(d => d.Path == directory);
                if (entry == null)
                {
                    _logService.Trace($@"""{directory}"" is not in the list.  Adding it...");
                    _recentlyUsedDirectories.Insert(0, new Models.Directory
                    {
                        Caption = GetCaption(directory),
                        Path = directory
                    });

                    _logService.Trace($@"Notifying {_observers.Count} of ""{directory}""...");
                    foreach (var observer in _observers) SendRecentlyUsedDirectories(observer);

                    // save the list
                    Save();
                }
                else if (_recentlyUsedDirectories.IndexOf(entry) > 0)
                {
                    _logService.Trace($@"Removing ""{directory}"" from list...");
                    _recentlyUsedDirectories.Remove(entry);

                    _logService.Trace($@"Inserting ""{directory}"" at the top of the list...");
                    _recentlyUsedDirectories.Insert(0, entry);

                    _logService.Trace($@"Notifying {_observers.Count} of ""{directory}""...");
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

        public string GetMostRecentlyUsedDirectory()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Returning path to the most recently used directory...");
                return _recentlyUsedDirectories.FirstOrDefault()?.Path;
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
            var stopWatch = new Stopwatch().StartStopwatch();

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