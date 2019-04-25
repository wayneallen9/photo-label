using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using PhotoLabel.Services;

using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace PhotoLabel.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region variables

        private CancellationTokenSource _loadPreviewCancellationTokenSource;
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
            var mainWindowViewModel = (MainWindowViewModel) DataContext;

            _logService.TraceEnter();
            try
            {
                _logService.Trace("Prompting for directory...");
                using (var folderBrowseDialog = new FolderBrowserDialog())
                {
                    _logService.Trace("Showing folder browse dialog...");
                    folderBrowseDialog.Description = @"Where are the photos?";
                    if (folderBrowseDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    {
                        _logService.Trace("User has cancelled dialog.  Exiting...");
                        return;
                    }

                    // get the selected path
                    var selectedPath = folderBrowseDialog.SelectedPath;

                    _logService.Trace($@"Calling view model method with directory ""{selectedPath}""...");
                    mainWindowViewModel.Open(selectedPath);
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

        private void ButtonSave_OnClick(object sender, RoutedEventArgs e)
        {
            var mainWindowViewModel = (MainWindowViewModel)DataContext;

            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if output path exists...");
                if (string.IsNullOrWhiteSpace(mainWindowViewModel.OutputPath) ||
                    !Directory.Exists(mainWindowViewModel.OutputPath))
                {
                    _logService.Trace("Output path does not exist.  Prompting for output path...");
                    SaveAs();
                }
                else
                {
                    _logService.Trace($@"Saving selected image to ""{mainWindowViewModel.OutputPath}""...");
                    mainWindowViewModel.Save(mainWindowViewModel.OutputPath);
                }
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

        private void SaveAs()
        {
            var mainWindowViewModel = (MainWindowViewModel)DataContext;

            _logService.TraceEnter();
            try
            {
                _logService.Trace("Prompting for directory...");
                using (var directoryBrowserDialog = new FolderBrowserDialog
                {
                    Description = @"Where are your photos?"
                })
                {
                    if (directoryBrowserDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    {
                        _logService.Trace(("User cancelled folder selection..."));
                        return;
                    }

                    _logService.Trace($@"Saving selected image to ""{directoryBrowserDialog.SelectedPath}""...");
                    mainWindowViewModel.Save(directoryBrowserDialog.SelectedPath);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ButtonSaveAs_OnClick(object sender, RoutedEventArgs e)
        {
            var mainWindowViewModel = (MainWindowViewModel)DataContext;

            _logService.TraceEnter();
            try
            {
                _logService.Trace("Prompting for new output path...");
                SaveAs();
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

        private void ListViewImages_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listViewImages = (ListView) sender;

            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if there are any selected images...");
                if (e.AddedItems.Count == 0)
                {
                    _logService.Trace("There are no selected images.  Exiting...");
                    return;
                }

                _logService.Trace("Ensuring that selected image is visible...");
                listViewImages.ScrollIntoView(e.AddedItems[0]);
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

        private void ListViewImages_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var listViewImages = (ListView)sender;

            _logService.TraceEnter();
            try
            {
                _logService.Trace("Cancelling any in progress preview loads...");
                _loadPreviewCancellationTokenSource?.Cancel();
                _loadPreviewCancellationTokenSource = new CancellationTokenSource();

                _logService.Trace("Getting bounds of list view...");
                var listViewBounds = new Rect(0, 0, listViewImages.ActualWidth, listViewImages.ActualHeight);

                _logService.Trace("Finding all visible images...");
                for (var p=listViewImages.Items.Count; p > 0;)
                {
                    // get the container for the item
                    var item = listViewImages.Items[--p];
                    var container = (ListViewItem) listViewImages.ItemContainerGenerator.ContainerFromItem(item);

                    // is the element visibly?
                    if (!container.IsVisible) continue;

                    // get the bounds of the item container
                    var itemBounds = container.TransformToAncestor(listViewImages)
                        .TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));

                    // is it outside the bounds?
                    if (!listViewBounds.Contains(itemBounds.TopLeft) &&
                        !listViewBounds.Contains(itemBounds.BottomRight)) continue;

                    // load this image
                    var imageViewModel = (ImageViewModel) container.DataContext;
                    imageViewModel.LoadPreview(_loadPreviewCancellationTokenSource.Token);
                }
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

        private void ListViewImages_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var listViewImages = (ListView)sender;

            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if there are any selected images...");
                if (listViewImages.SelectedItem == null)
                {
                    _logService.Trace("There are no selected images.  Exiting...");
                    return;
                }

                _logService.Trace("Ensuring that selected image is visible...");
                listViewImages.ScrollIntoView(listViewImages.SelectedItem);
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