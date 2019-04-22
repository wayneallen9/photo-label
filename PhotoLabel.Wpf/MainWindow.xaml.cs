using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PhotoLabel.Services;

using DialogResult = System.Windows.Forms.DialogResult;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace PhotoLabel.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region variables
        private readonly ILogService _logService;
        #endregion

        public MainWindow(
            ILogService logService)
        {
            // save dependencies
            _logService = logService;

            InitializeComponent();
        }

        private void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Opening new directory...");
                Open();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ErrorText, Properties.Resources.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void Open()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Prompting for directory...");
                using (var folderBrowseDialog = new FolderBrowserDialog())
                {
                    _logService.Trace("Showing folderr browse dialog...");
                    folderBrowseDialog.Description = @"Where are the photos?";
                    if (folderBrowseDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    {
                        _logService.Trace("User has cancelled dialog.  Exiting...");
                        return;
                    }

                    // get the selected path
                    var selectedPath = folderBrowseDialog.SelectedPath;

                    _logService.Trace($@"Calling view model method with directory ""{selectedPath}""...");
                    ((MainWindowViewModel)DataContext).Open(selectedPath);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Opening new directory...");
                Open();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ErrorText, Properties.Resources.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}
