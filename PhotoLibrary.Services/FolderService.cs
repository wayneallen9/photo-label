using PhotoLabel.Services.Models;
using System.Collections.Generic;
using System.IO;

namespace PhotoLabel.Services
{
    public class FolderService : IFolderService
    {
        public FolderService(
            ILogService logService)
        {
            // save dependencies
            _logService = logService;
        }

        private string GetCaption(string path)
        {
            _logService.TraceEnter();
            try
            {
                // was a directory provided?
                if (string.IsNullOrWhiteSpace(path)) return string.Empty;

                // is it less than 20 characters?
                if (path.Length <= 20) return path;

                // build it back up
                var root = path.Substring(0, path.IndexOf(Path.DirectorySeparatorChar, 2) + 1);
                var branch = path.Substring(path.LastIndexOf(Path.DirectorySeparatorChar));

                return $"{root}...{branch}";
            }
            finally
            {
                _logService.TraceExit();
            }
        }


        public Folder Open(string path)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Checking if ""{path}"" exists...");
                if (!Directory.Exists(path))
                {
                    _logService.Trace($@"""{path}"" does not exist.  Throwing exception...");
                    throw new DirectoryNotFoundException();
                }

                _logService.Trace($@"Creating path for ""{path}""...");
                var folder = new Folder
                {
                    Caption = GetCaption(path),
                    Path = path,
                    SubFolders = new List<SubFolder>()
                };

                _logService.Trace($@"Getting subfolders of ""{path}""...");
                var subfolders = Directory.EnumerateDirectories(path);

                foreach (var subfolder in subfolders)
                {
                    _logService.Trace($@"Adding ""{subfolder}"" to list of subfolders...");
                    folder.SubFolders.Add(new SubFolder
                    {
                        Path = subfolder.Substring(path.Length + 1)
                    });
                }

                return folder;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        #region variables

        private readonly ILogService _logService;
        #endregion
    }
}