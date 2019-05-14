using System.Windows;
using System.Windows.Forms;
using Shared;
using Shared.Attributes;

namespace PhotoLabel.Services
{
    [Singleton]
    public class DialogService : IDialogService
    {
        #region variables

        private readonly ILogger _logger;
        #endregion

        public DialogService(
            ILogger logger)
        {
            // save dependencies
            _logger = logger;
        }

        public string Browse(string description, string defaultFolder)
        {
            using (var logger = _logger.Block()) {
                using (var folderBrowserDialog = new FolderBrowserDialog
                {
                    Description = description,
                    SelectedPath = defaultFolder
                })
                {
                    logger.Trace("Prompting user for folder...");
                    return folderBrowserDialog.ShowDialog() != DialogResult.OK
                        ? null
                        : folderBrowserDialog.SelectedPath;
                }
            
            }
        }

        public bool Confirm(string text, string caption)
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Displaying confirmation message to user...");
                return System.Windows.MessageBox.Show(text, caption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
            
            }
        }

        public void Error(string text)
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Displaying error message to user...");
                System.Windows.MessageBox.Show(text, @"An Unexpected Error Occurred", MessageBoxButton.OK, MessageBoxImage.Error);
            
            }
        }
    }
}