using System.Windows;
using System.Windows.Forms;

namespace PhotoLabel.Services
{
    public class DialogService : IDialogService
    {
        #region variables

        private readonly ILogService _logService;
        #endregion

        public DialogService(
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

        public bool Confirm(string text, string caption)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Displaying confirmation message to user...");
                return System.Windows.MessageBox.Show(text, caption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void Error(string text)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Displaying error message to user...");
                System.Windows.MessageBox.Show(text, @"An Unexpected Error Occurred", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}