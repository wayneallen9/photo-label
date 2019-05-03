using PhotoLabel.Services;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace PhotoLabel.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
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
                // ignored
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
                    imageViewModel.LoadPreview(false, _loadPreviewCancellationTokenSource.Token);
                }
            }
            catch (Exception ex)
            {
                // ignored
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
                // ignored
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}