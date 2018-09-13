using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace PhotoLibrary.Services
{
    public class RecentlyUsedFilesService : IRecentlyUsedFilesService
    {
        #region delegates
        #endregion

        #region events
        #endregion

        #region variables
        private readonly ILogService _logService;
        #endregion

        public RecentlyUsedFilesService(
            ILogService logService)
        {
            // save the dependency injections
            _logService = logService;

            // load the list from the properties
            Filenames = new List<string>(Properties.Settings.Default.RecentlyUsedFiles ?? new string[] { });
        }

        public List<string> Filenames { get; }

        public string GetCaption(string filename)
        {
            _logService.TraceEnter();
            try {
                // was a filename provided?
                if (string.IsNullOrWhiteSpace(filename)) return string.Empty;

                // is it less than 20 characters?
                if (filename.Length <= 20) return filename;

                // build it back up
                var root = filename.Substring(0, filename.IndexOf(Path.DirectorySeparatorChar, 2) + 1);
                var branch = filename.Substring(filename.LastIndexOf(Path.DirectorySeparatorChar));

                return $"{root}...{branch}";
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void Open(string filename)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Adding \"{filename}\" to the list of recently used files...");

                // move it to the top
                Filenames.Remove(filename);

                // add it at the top of the list
                Filenames.Insert(0, filename);

                // keep a maximum of 10 entries
                if (Filenames.Count > 10)
                    Filenames.RemoveAt(10);

                // save the change
                Properties.Settings.Default.RecentlyUsedFiles = Filenames.ToArray();
                Properties.Settings.Default.Save();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void Remove(string filename)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Removing \"{filename}\" from the list of recently used files...");
                Filenames.Remove(filename);

                _logService.Trace("Saving list of recently used files...");
                Properties.Settings.Default.RecentlyUsedFiles = Filenames.ToArray();
                Properties.Settings.Default.Save();
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}