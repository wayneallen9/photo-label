using PhotoLabel.Services.Models;
using System.Collections.Generic;
using System.IO;
using Shared;
using Shared.Attributes;

namespace PhotoLabel.Services
{
    [Singleton]
    public class FolderService : IFolderService
    {
        public FolderService(
            ILogger logger)
        {
            // save dependencies
            _logger = logger;
        }

        private string GetCaption(string path)
        {
            using (var logger = _logger.Block()) {
                // was a directory provided?
                if (string.IsNullOrWhiteSpace(path)) return string.Empty;

                // is it less than 20 characters?
                if (path.Length <= 20) return path;

                // build it back up
                var root = path.Substring(0, path.IndexOf(Path.DirectorySeparatorChar, 2) + 1);
                var branch = path.Substring(path.LastIndexOf(Path.DirectorySeparatorChar));

                return $"{root}...{branch}";
            
            }
        }


        public Folder Open(string path)
        {
            using (var logger = _logger.Block()) {
                logger.Trace($@"Checking if ""{path}"" exists...");
                if (!Directory.Exists(path))
                {
                    logger.Trace($@"""{path}"" does not exist.  Throwing exception...");
                    throw new DirectoryNotFoundException();
                }

                logger.Trace($@"Creating path for ""{path}""...");
                var folder = new Folder
                {
                    Caption = GetCaption(path),
                    Path = path,
                    SubFolders = new List<SubFolder>()
                };

                logger.Trace($@"Getting subfolders of ""{path}""...");
                var subfolders = Directory.EnumerateDirectories(path);

                foreach (var subfolder in subfolders)
                {
                    logger.Trace($@"Adding ""{subfolder}"" to list of subfolders...");
                    folder.SubFolders.Add(new SubFolder
                    {
                        Path = subfolder.Substring(path.Length + 1)
                    });
                }

                return folder;
            
            }
        }

        #region variables

        private readonly ILogger _logger;
        #endregion
    }
}