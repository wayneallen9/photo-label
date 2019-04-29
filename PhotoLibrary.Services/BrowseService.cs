using System.Windows.Forms;

namespace PhotoLabel.Services
{
    public class BrowseService : IBrowseService
    {
        #region variables

        private readonly ILogService _logService;
        #endregion

        public BrowseService(
            ILogService logService)
        {
            // save dependencies
            _logService = logService;
        }

        public string Browse(string description, string defaultFolder)
        {
            _logService.TraceEnter();
            try
            {
                using (var folderBrowserDialog = new FolderBrowserDialog
                {
                    Description = description,
                    SelectedPath = defaultFolder
                })
                {
                    _logService.Trace("Prompting user for folder...");
                    return folderBrowserDialog.ShowDialog() != DialogResult.OK
                        ? null
                        : folderBrowserDialog.SelectedPath;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}