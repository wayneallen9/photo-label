using PhotoLabel.Services.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
namespace PhotoLabel.Services
{
    public class RecentlyUsedDirectoriesService : IRecentlyUsedFoldersService
    {
        #region delegates
        #endregion

        #region events
        #endregion

        #region variables
        private readonly ILogService _logService;
        #endregion

        public RecentlyUsedDirectoriesService(
            ILogService logService)
        {
            // save the dependency injections
            _logService = logService;
        }

        private string GetCaption(string folder)
        {
            _logService.TraceEnter();
            try {
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

        public List<FolderModel> Load()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Getting path to recently used directories file...");
                string filename = GetFilename();

                _logService.Trace($@"Checking if recently used directories file ""{filename}"" exists...");
                if (!File.Exists(filename))
                {
                    _logService.Trace($@"""{filename}"" does not exist.  Returning...");
                    return new List<FolderModel>();
                }
                _logService.Trace($@"Recently used directories file ""{filename}"" exists");

                _logService.Trace($@"Deserialising ""{filename}""...");
                using (var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    // create the XML serializer
                    var serializer = new XmlSerializer(typeof(List<FolderModel>));

                    // now deserialize it
                    return serializer.Deserialize(fileStream) as List<FolderModel>;
                }
            }
            catch (InvalidOperationException)
            {
                // the XML is invalid, ignore it
                return new List<FolderModel>();
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
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Photo Label", "Recently Used Files.xml");
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public List<FolderModel> Add(string folder, List<FolderModel> folders)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Adding ""{folder}"" to the list of recently used directories...");

                _logService.Trace($@"Checking if ""{folder}"" is already in the list...");
                var entry = folders.FirstOrDefault(d => d.Path == folder);
                if (entry == null)
                {
                    _logService.Trace($@"""{folder}"" is not in the list.  Adding it...");
                    folders.Insert(0, new FolderModel
                    {
                        Caption = GetCaption(folder),
                        Path = folder
                    });
                }
                else
                {
                    _logService.Trace($@"Removing ""{folder}"" from list...");
                    folders.Remove(entry);

                    _logService.Trace($@"Inserting ""{folder}"" at the top of the list...");
                    folders.Insert(0, entry);
                }

                // save the list
                Save(folders);

                return folders.Take(10).ToList();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public List<FolderModel> Save(List<FolderModel> folders)
        {
            _logService.TraceEnter();
            try
            {
                // save the change
                var filename = GetFilename();

                _logService.Trace($@"Getting directory for ""{filename}""...");
                var directory = Path.GetDirectoryName(filename);
                if (directory != null)
                {
                    // create the folder
                    _logService.Trace($@"Ensuring that all parent directories exist for ""{filename}""...");
                    Directory.CreateDirectory(directory);
                }

                _logService.Trace($@"Saving list of recently used directories to ""{filename}""...");
                using (var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    // create the serializer
                    var serializer = new XmlSerializer(folders.GetType());

                    // serialize the list
                    serializer.Serialize(fileStream, folders);
                }

                return folders;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public List<FolderModel> Remove(string folder, List<FolderModel> folders)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Removing ""{folder}"" from the list of recently used directories...");
                folders.RemoveAll(d => d.Path == folder);

                _logService.Trace("Saving list of recently used files...");
                Save(folders);

                return folders;
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}