using PhotoLabel.Services.Models;
using Shared;
using Shared.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace PhotoLabel.Services
{
    [Singleton]
    public class RecentlyUsedFoldersService : IRecentlyUsedFoldersService
    {
        #region delegates

        #endregion

        #region variables

        private readonly ILogger _logger;
        private readonly List<IRecentlyUsedDirectoriesObserver> _observers;
        private readonly List<Folder> _recentlyUsedDirectories;
        private readonly IXmlFileSerialiser _xmlFileSerialiser;

        #endregion

        public RecentlyUsedFoldersService(
            ILogger logger,
            IXmlFileSerialiser xmlFileSerialiser)
        {
            // save the dependency injections
            _logger = logger;
            _xmlFileSerialiser = xmlFileSerialiser;

            // initialise variables
            _observers = new List<IRecentlyUsedDirectoriesObserver>();
            _recentlyUsedDirectories = new List<Folder>();
        }

        private void Clear()
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Clearing current list of recently used files...");
                _recentlyUsedDirectories.Clear();

                logger.Trace($"Notifiying {_observers.Count} observers that list has been cleared...");
                foreach (var observer in _observers) observer.OnClear();
            
            }
        }

        public void Load(CancellationToken cancellationToken)
        {
            using (var logger = _logger.Block()) {
                try
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    Clear();

                    if (cancellationToken.IsCancellationRequested) return;
                    logger.Trace("Getting path to recently used directories file...");
                    var filename = GetFilename();

                    if (cancellationToken.IsCancellationRequested) return;
                    logger.Trace($@"Loading recently used directories from ""{filename}""...");
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
                    logger.Trace($"Notifying {_observers.Count} observers of error...");
                    foreach (var observer in _observers)
                    {
                        observer.OnError(ex);
                    }

                }
            }
        }

        private string GetFilename()
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Building path to recently used folders file...");
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Photo Label",
                    "Recently Used Files.xml");
            
            }
        }

        public void Add(Folder folder)
        {
            using (var logger = _logger.Block()) {
                logger.Trace($@"Adding ""{folder.Path}"" to the list of recently used directories...");

                logger.Trace($@"Checking if ""{folder.Path}"" is already in the list...");
                var entry = _recentlyUsedDirectories.FirstOrDefault(d => d.Path == folder.Path);
                if (entry == null)
                {
                    logger.Trace($@"""{folder.Path}"" is not in the list.  Adding it...");
                    _recentlyUsedDirectories.Insert(0, folder);

                    logger.Trace($@"Notifying {_observers.Count} of ""{folder.Path}""...");
                    foreach (var observer in _observers) SendRecentlyUsedDirectories(observer);

                    // save the list
                    Save();
                }
                else
                {
                    logger.Trace($@"Removing ""{folder.Path}"" from list...");
                    _recentlyUsedDirectories.Remove(entry);

                    logger.Trace($@"Inserting ""{folder.Path}"" at the top of the list...");
                    _recentlyUsedDirectories.Insert(0, folder);

                    logger.Trace($@"Notifying {_observers.Count} of ""{folder.Path}""...");
                    foreach (var observer in _observers) SendRecentlyUsedDirectories(observer);

                    // save the list
                    Save();
                }
            
            }
        }

        public Folder GetMostRecentlyUsedDirectory()
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Returning path to the most recently used directory...");
                return _recentlyUsedDirectories.FirstOrDefault();
            
            }
        }

        public string GetMostRecentlyUsedFile()
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Returning path to the most recently used file...");
                return _recentlyUsedDirectories.FirstOrDefault()?.Filename;
            
            }
        }

        private void Save()
        {
            using (var logger = _logger.Block()) {
                // save the change
                var filename = GetFilename();

                logger.Trace($@"Saving recently used directories to ""{filename}""...");
                _xmlFileSerialiser.Serialise(_recentlyUsedDirectories, filename);
            
            }
        }

        public void SetLastSelectedFile(string filename)
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Getting most recently used directory...");
                var mostRecentlyUsedDirectory = _recentlyUsedDirectories.First();

                logger.Trace(
                    $@"Setting most recently used file for ""{mostRecentlyUsedDirectory.Path}"" to ""filename""...");
                mostRecentlyUsedDirectory.Filename = filename;

                Save();
            
            }
        }

        public IDisposable Subscribe(IRecentlyUsedDirectoriesObserver observer)
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Checking if observer is already subscribed...");
                if (_observers.Contains(observer))
                    return new Unsubscriber<IRecentlyUsedDirectoriesObserver>(_observers, observer);

                logger.Trace("Observer is not subscribed.  Subscribing...");
                _observers.Add(observer);

                SendRecentlyUsedDirectories(observer);

                return new Unsubscriber<IRecentlyUsedDirectoriesObserver>(_observers, observer);
            
            }
        }

        private void SendRecentlyUsedDirectories(IRecentlyUsedDirectoriesObserver observer)
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Notifying observer to clear list...");
                observer.OnClear();

                logger.Trace("Notifying observer of current state...");
                foreach (var directory in _recentlyUsedDirectories)
                {
                    logger.Trace($@"Notifying observer of ""{directory}""...");
                    observer.OnNext(directory);
                }
            
            }
        }
    }
}