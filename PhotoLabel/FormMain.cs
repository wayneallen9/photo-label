using PhotoLabel.Services;
using PhotoLabel.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace PhotoLabel
{
    public partial class FormMain : Form
    {
        #region delegates
        private delegate void ActionDelegate(Action action);
        private delegate void DefaultDelegate();
        private delegate void ShowPictureDelegate(ImageViewModel imageViewModel);
        #endregion

        #region variables
        private readonly IImageMetadataService _imageMetadataService;
        private readonly IImageService _imageService;
        private readonly List<ImageViewModel> _loadPreviewImageQueue = new List<ImageViewModel>();
        private readonly ILocaleService _localeService;
        private readonly ILogService _logService;
        private readonly MainFormViewModel _mainFormViewModel;
        private CancellationTokenSource _openFolderCancellationTokenSource;
        private readonly IRecentlyUsedFilesService _recentlyUsedFilesService;
        #endregion

        public FormMain(
            IImageService imageService,
            ILocaleService localeService,
            ILogService logService,
            MainFormViewModel mainFormViewModel,
            IImageMetadataService imageMetadataService,
            IRecentlyUsedFilesService recentlyUsedFilesService)
        {
            // save dependencies
            _imageMetadataService = imageMetadataService;
            _imageService = imageService;
            _localeService = localeService;
            _logService = logService;
            _recentlyUsedFilesService = recentlyUsedFilesService;

            InitializeComponent();

            // initialise the model binding
            bindingSourceMain.DataSource = _mainFormViewModel = mainFormViewModel;

            // initialise the recently used files
            DrawRecentlyUsedFiles();
        }

        private void CentrePictureBox()
        {
            _logService.TraceEnter();
            try
            {
                // this must run on the UI thread
                _logService.Trace($"Image size is {pictureBoxImage.Width}px x {pictureBoxImage.Height}px");
                if (pictureBoxImage.Height < panelSize.Height)
                    pictureBoxImage.Top = (panelSize.Height - pictureBoxImage.Height) / 2;
                else
                    pictureBoxImage.Top = 0;

                if (pictureBoxImage.Width < panelSize.Width)
                    pictureBoxImage.Left = (panelSize.Width - pictureBoxImage.Width) / 2;
                else
                    pictureBoxImage.Left = 0;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void Invoke(Action action)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on the UI thread...");
                if (InvokeRequired)
                {
                    _logService.Trace("Not running on the UI thread");
                    Invoke(new ActionDelegate(Invoke), action);
                }
                else
                {
                    _logService.Trace("Running on the UI thread.  Executing action...");
                    action();
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void MainFormViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _logService.TraceEnter();

            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (InvokeRequired)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoke(new PropertyChangedEventHandler(MainFormViewModel_PropertyChanged), sender, e);

                    return;
                }

                _logService.Trace("Running on UI thread.  Getting source object...");
                var mainFormViewModel = sender as MainFormViewModel;

                _logService.Trace("Checking which property has updated...");
                switch (e.PropertyName)
                {
                    case "OutputPath":
                        _logService.Trace("Output path has changed");
                        ShowOutputPath();

                        break;
                    case "Zoom":
                        _logService.Trace("Zoom has changed");

                        // show the updated zoom
                        ShowZoom();

                        break;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OpenFolder()
        {
            _logService.TraceEnter();
            try
            {
                // show the dialog
                if (folderBrowserDialogImages.ShowDialog() == DialogResult.OK)
                    OpenFolder(folderBrowserDialogImages.SelectedPath);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ZoomImage()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if there is a current image showing...");
                if (pictureBoxImage.Image == null)
                {
                    _logService.Trace("No image is showing.  Exiting...");
                    return;
                }

                _logService.Trace($"Zooming to {_mainFormViewModel.Zoom}%...");
                pictureBoxImage.Height = pictureBoxImage.Image.Height * _mainFormViewModel.Zoom / 100;
                pictureBoxImage.Width = pictureBoxImage.Image.Width * _mainFormViewModel.Zoom / 100;

                _logService.Trace("Centering image...");
                if (pictureBoxImage.Width < panelSize.Width)
                {
                    pictureBoxImage.Left = (panelSize.Width - pictureBoxImage.Width) / 2;
                }
                else
                {
                    pictureBoxImage.Left = 0;
                }
                if (pictureBoxImage.Height < panelSize.Height)
                {
                    pictureBoxImage.Top = (panelSize.Height - pictureBoxImage.Height) / 2;
                }
                else
                {
                    pictureBoxImage.Top = 0;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        /// <summary>
        /// Cancels any in progress load then loads all images from the specified path.
        /// </summary>
        /// <param name="folderPath">The source path for the images.</param>
        private void OpenFolder(string folderPath)
        {
            _logService.TraceEnter();
            try
            {
                // cancel any in progress load
                if (_openFolderCancellationTokenSource != null) _openFolderCancellationTokenSource.Cancel();

                // create a new cancellation token source
                _openFolderCancellationTokenSource = new CancellationTokenSource();

                // show the Ajax image
                ShowAjaxImage();

                // clear the lists
                listViewPreview.Clear();
                imageListLarge.Images.Clear();

                // return to the start position
                bindingSourceImages.MoveFirst();

                // run the load on another thread
                Task.Run(() => OpenFolderThread(folderPath, _openFolderCancellationTokenSource.Token), _openFolderCancellationTokenSource.Token);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void SetToolbarStatus()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if there is a current image...");
                var currentImageViewModel = bindingSourceImages.Position > -1 ? bindingSourceImages.Current as ImageViewModel : null;

                colourToolStripMenuItem.Enabled =
                    fontToolStripMenuItem.Enabled =
                    rotateLeftToolStripMenuItem.Enabled =
                    rotateRightToolStripMenuItem.Enabled =
                    saveToolStripMenuItem.Enabled =
                    saveAsToolStripMenuItem.Enabled = currentImageViewModel != null;
                toolStripButtonColour.Enabled =
                    toolStripButtonDontSave.Enabled =
                    toolStripButtonFont.Enabled =
                    toolStripButtonRotateLeft.Enabled =
                    toolStripButtonRotateRight.Enabled =
                    toolStripButtonSave.Enabled =
                    toolStripButtonSaveAs.Enabled = currentImageViewModel != null;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowAjaxImage()
        {
            _logService.TraceEnter();
            try
            {
                // show the Ajax icon
                _logService.Trace("Displaying ajax resource...");
                pictureBoxImage.Image = Properties.Resources.ajax;
                pictureBoxImage.Height = Properties.Resources.ajax.Height;
                pictureBoxImage.Width = Properties.Resources.ajax.Width;

                _logService.Trace("Centering ajax resource...");
                CentrePictureBox();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowLocation(ImageViewModel imageViewModel)
        {
            _logService.TraceEnter();
            try
            {
                toolStripButtonLocation.Enabled = imageViewModel?.Latitude != null && imageViewModel?.Longitude != null;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowOutputPath()
        {
            _logService.TraceEnter();
            try
            {
                if (string.IsNullOrWhiteSpace(_mainFormViewModel.OutputPath))
                {
                    _logService.Trace("Clearing output directory display...");
                    toolStripStatusLabelOutputDirectory.Text = string.Empty;
                }
                else
                {
                    _logService.Trace($"Showing output directory \"{_mainFormViewModel.OutputPath}\"...");
                    toolStripStatusLabelOutputDirectory.Text = _mainFormViewModel.OutputPath;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowPicture(ImageViewModel imageViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (InvokeRequired)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoke(new ShowPictureDelegate(ShowPicture), imageViewModel);

                    return;
                }
                _logService.Trace("Running on UI thread");

                if (imageViewModel.Image != null)
                {
                    // show the image
                    pictureBoxImage.Image = imageViewModel.Image;

                    // zoom the image
                    ZoomImage();

                    // position it in the form
                    CentrePictureBox();
                }
                else
                {
                    ShowAjaxImage();
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowPreview(ImageViewModel imageViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Updating preview image for \"{imageViewModel.Filename}\"...");

                // if there is a preview queued to be loaded, cancel it
                lock (_loadPreviewImageQueue)
                {
                    _loadPreviewImageQueue.RemoveAll(i => i.Filename == imageViewModel.Filename);
                }

                lock (imageListLarge)
                {
                    // remove the existing image
                    imageListLarge.Images.RemoveByKey(imageViewModel.Filename);
                    imageListLarge.Images.Add(imageViewModel.Filename, imageViewModel.Preview ?? Properties.Resources.loading);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowProgress()
        {
            toolStripStatusLabelStatus.Text = $"{bindingSourceImages.Position + 1} of {bindingSourceImages.Count}";
        }

        private void ShowCaptionAlignment(ImageViewModel imageViewModel)
        {
            _logService.TraceEnter();
            try
            {
                checkBoxTopLeft.Checked = imageViewModel?.CaptionAlignment == CaptionAlignments.TopLeft;
                checkBoxTopCentre.Checked = imageViewModel?.CaptionAlignment == CaptionAlignments.TopCentre;
                checkBoxTopRight.Checked = imageViewModel?.CaptionAlignment == CaptionAlignments.TopRight;
                checkBoxLeft.Checked = imageViewModel?.CaptionAlignment == CaptionAlignments.MiddleLeft;
                checkBoxCentre.Checked = imageViewModel?.CaptionAlignment == CaptionAlignments.MiddleCentre;
                checkBoxRight.Checked = imageViewModel?.CaptionAlignment == CaptionAlignments.MiddleRight;
                checkBoxBottomLeft.Checked = imageViewModel?.CaptionAlignment == CaptionAlignments.BottomLeft;
                checkBoxBottomCentre.Checked = imageViewModel?.CaptionAlignment == CaptionAlignments.BottomCentre;
                checkBoxBottomRight.Checked = imageViewModel?.CaptionAlignment == CaptionAlignments.BottomRight;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowZoom()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Updating zoom to {_mainFormViewModel.Zoom}%...");
                toolStripComboBoxZoom.Text = string.Format("{0:P0}", _mainFormViewModel.Zoom / 100f);


                _logService.Trace("Resizing picture box...");
                ZoomImage();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OpenFolderThread(string folderPath, CancellationToken cancellationToken)
        {
            _logService.TraceEnter();

            try
            {
                // load the images from this folder
                if (cancellationToken.IsCancellationRequested) return;
                _logService.Trace($"Loading images from \"{folderPath}\"...");
                var loaded = _mainFormViewModel.Open(folderPath);

                // were there any images?
                if (cancellationToken.IsCancellationRequested) return;
                if (loaded == 0)
                {
                    _logService.Trace($"No images were found in \"{folderPath}\"");
                    Invoke(() =>
                    {
                        // let the user know that it failed
                        MessageBox.Show("No photos found", "Photo Label", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    });
                }
                else
                {
                    // return to the start
                    _logService.Trace("Resetting image position...");
                    Invoke(() =>
                    {
                        bindingSourceImages.MoveFirst();
                    });

                    // add it to the list of recently used files
                    _logService.Trace($"Adding \"{folderPath}\" to the list of recently used files...");
                    _recentlyUsedFilesService.Open(folderPath);

                    Invoke(new Action(() =>
                    {
                        // redraw the list of recently used files
                        DrawRecentlyUsedFiles();

                        // redraw the images
                        bindingSourceMain.ResetBindings(false);
                    }));

                    Invoke(() =>
                    {
                        // only update the display after they are all loaded
                        listViewPreview.SuspendLayout();

                        // add the preview list items
                        _logService.Trace("Creating preview list items...");
                        foreach (var imageListModel in _mainFormViewModel.Images)
                        {
                            // has the user cancelled the load?
                            if (cancellationToken.IsCancellationRequested) break;

                            // add the event handling
                            imageListModel.PropertyChanged += ImageViewModel_PropertyChanged;

                            // add the preview image for this item
                            listViewPreview.Items.Add(new ListViewItem
                            {
                                ImageKey = imageListModel.Filename,
                                Selected = bindingSourceImages.Position == listViewPreview.Items.Count,
                                ToolTipText = imageListModel.Filename
                            });

                            // set a default image before the actual image is loaded
                            imageListLarge.Images.Add(imageListModel.Filename, Properties.Resources.loading);
                        }

                        // now draw it
                        listViewPreview.ResumeLayout();
                    });

                    lock (_loadPreviewImageQueue)
                    {
                        // clear any existing loads
                        _loadPreviewImageQueue.Clear();

                        // add all of the images
                        _loadPreviewImageQueue.AddRange(_mainFormViewModel.Images);

                        // start loading on another thread
                        Task.Factory.StartNew(() => LoadPreviewImages(), TaskCreationOptions.LongRunning);
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                Invoke(() => MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error));
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ImageViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _logService.TraceEnter();

            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (InvokeRequired)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoke(new PropertyChangedEventHandler(ImageViewModel_PropertyChanged), sender, e);

                    return;
                }

                _logService.Trace("Running on UI thread.  Getting source image...");
                var imageViewModel = sender as ImageViewModel;

                _logService.Trace("Checking if there is a current image...");
                var currentImageViewModel = bindingSourceImages.Position != -1 ? bindingSourceImages.Current as ImageViewModel : null;

                _logService.Trace("Checking which property has updated...");
                switch (e.PropertyName)
                {
                    case "Caption":
                        _logService.Trace("Caption has been updated");
                        bindingSourceImages.ResetBindings(false);

                        break;
                    case "CaptionAlignment":
                        _logService.Trace("Caption alignment has been updated");
                        ShowCaptionAlignment(imageViewModel);

                        break;
                    case "Image":
                        _logService.Trace("Image has been updated");
                        _logService.Trace($"Checking if \"{imageViewModel.Filename}\" is current image...");
                        if (imageViewModel.Filename != currentImageViewModel?.Filename)
                        {
                            _logService.Trace($"\"{imageViewModel.Filename}\" is not the current image.  Exiting...");
                            return;
                        }

                        _logService.Trace($"Showing image for \"{imageViewModel.Filename}\"...");
                        ShowPicture(imageViewModel);

                        _logService.Trace("Setting toolbar status...");
                        SetToolbarStatus();

                        break;
                    case "Latitude":
                    case "Longitude":
                        ShowLocation(imageViewModel);

                        break;
                    case "Preview":
                        _logService.Trace("Preview image has been updated");
                        ShowPreview(imageViewModel);

                        break;
                    case "Rotation":
                        ShowCaptionAlignment(imageViewModel);

                        break;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                OpenFolder();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ToolStripButtonOpen_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                OpenFolder();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ToolStripButtonFont_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Changing font...");
                ChangeFont();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ChangeFont()
        {
            _logService.TraceEnter();
            try
            {
                // get the current image
                var currentImageViewModel = bindingSourceImages.Position > -1 ? bindingSourceImages.Current as ImageViewModel : null;

                // set the current font
                _logService.Trace($"Defaulting to current image font \"{currentImageViewModel.Font.Name}\"...");
                fontDialog.Font = currentImageViewModel.Font;

                // now show the dialog
                if (fontDialog.ShowDialog() == DialogResult.OK)
                {
                    // save the new font as the default font
                    _logService.Trace("Updating default font...");
                    _mainFormViewModel.Font = fontDialog.Font;

                    // update the font on the current image
                    _logService.Trace("Updating font on current image...");
                    currentImageViewModel.Font = fontDialog.Font;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void DrawRecentlyUsedFiles()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Removing existing recently used file menu options...");

                // reset and remove any already existing options
                toolStripMenuItemSeparator.Visible = false;
                for (var i = 0; i < toolStripMenuItemFile.DropDownItems.Count;)
                {
                    // only recently used files have a tag
                    if (toolStripMenuItemFile.DropDownItems[i].Tag != null)
                        toolStripMenuItemFile.DropDownItems.RemoveAt(i);
                    else
                        i++;
                }

                // set the recently used files
                _logService.Trace($"Adding {_recentlyUsedFilesService.Filenames.Count} recently used file menu options...");
                foreach (var recentlyUsedFile in _recentlyUsedFilesService.Filenames)
                {
                    // show the separator
                    toolStripMenuItemSeparator.Visible = true;

                    // create the new menu item
                    var menuItem = new ToolStripMenuItem
                    {
                        Text = _recentlyUsedFilesService.GetCaption(recentlyUsedFile),
                        ToolTipText = recentlyUsedFile,
                        Tag = recentlyUsedFile
                    };

                    menuItem.Click += (s, e) =>
                    {
                        RecentlyUsedFile_Open(recentlyUsedFile);
                    };

                    // add it to the menu
                    toolStripMenuItemFile.DropDownItems.Insert(toolStripMenuItemFile.DropDownItems.Count - 2, menuItem);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ToolStripComboBoxZoom_Validated(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                // set the new zoom
                _mainFormViewModel.Zoom = _localeService.PercentageTryParse(toolStripComboBoxZoom.Text, out decimal percentage) ? (int)percentage : 100;
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ToolStripComboBoxZoom_SelectedIndexChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                ValidateChildren();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ToolStripButtonColour_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Changing colour...");
                ChangeColour();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ChangeColour()
        {
            _logService.TraceEnter();
            try
            {
                // get the current image
                var currentImageViewModel = bindingSourceImages.Current as ImageViewModel;

                // set the default color
                _logService.Trace("Defaulting to current image colour...");
                colorDialog.Color = currentImageViewModel.Color.Value;

                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    // update the color
                    _logService.Trace("Updating default colour...");
                    _mainFormViewModel.Color = colorDialog.Color;

                    // update the colour on any existing image
                    _logService.Trace("Updating colour on current image...");
                    currentImageViewModel.Color = colorDialog.Color;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void FormMain_SizeChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                // save the window state
                _mainFormViewModel.WindowState = WindowState;

                // recentre the photo
                CentrePictureBox();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ToolStripButtonSave_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Saving file...");
                Save();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void RecentlyUsedFile_Open(string filename)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Checking if the file exists...");
                if (Directory.Exists(filename))
                {
                    _logService.Trace($"Opening \"{filename}\" from recently used file list...");
                    OpenFolder(filename);
                }
                else
                {
                    _logService.Trace($"\"{filename}\" does not exist");
                    if (MessageBox.Show($"\"{filename}\" could not be found.  Do you wish to remove it from the list of recently used folders?", "Folder Not Found", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        _logService.Trace($"Removing \"{filename}\" from list of recently used files...");
                        _recentlyUsedFilesService.Remove(filename);

                        _logService.Trace("Redrawing list of recently used files...");
                        DrawRecentlyUsedFiles();
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void Save()
        {
            _logService.TraceEnter();
            try
            {
                // do we have a path to save to already?
                if (!string.IsNullOrWhiteSpace(_mainFormViewModel.OutputPath) &&
                    Directory.Exists(_mainFormViewModel.OutputPath))
                {
                    // Save the current image
                    _logService.Trace($"Saving to \"{_mainFormViewModel.OutputPath}\"...");
                    SaveImage();
                }
                else
                {
                    // set the default path
                    folderBrowserDialogSave.SelectedPath = _mainFormViewModel.OutputPath;

                    // do we have a path to save to already?
                    if (folderBrowserDialogSave.ShowDialog() == DialogResult.OK)
                    {
                        // save the save path
                        _mainFormViewModel.OutputPath = folderBrowserDialogSave.SelectedPath;

                        // save the current image
                        SaveImage();

                        // refresh the display
                        bindingSourceMain.ResetBindings(false);
                    }
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void SaveImage()
        {
            _logService.TraceEnter();
            try
            {
                // get the current image
                var currentImageViewModel = bindingSourceImages.Current as ImageViewModel;

                // are we overwriting the original?
                var outputPath = Path.Combine(_mainFormViewModel.OutputPath, currentImageViewModel.FilenameWithoutPath);
                _logService.Trace($"Destination file is \"{outputPath}\"");
                if (outputPath != currentImageViewModel.Filename)
                {
                    // try and save the image
                    if (currentImageViewModel.Save(_mainFormViewModel.OutputPath, false))
                    {
                        // save the metadata
                        _imageMetadataService.Save(currentImageViewModel.Caption, currentImageViewModel.CaptionAlignment.Value, currentImageViewModel.Font, (Color)currentImageViewModel.Color, currentImageViewModel.Rotation, currentImageViewModel.Filename);

                        // go to the next image
                        bindingSourceImages.MoveNext();
                    }
                    else if (MessageBox.Show($"The file \"{currentImageViewModel.FilenameWithoutPath}\" already exists.  Do you wish to overwrite it?", "Overwrite file?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        // overwrite the existing image
                        currentImageViewModel.Save(_mainFormViewModel.OutputPath, true);

                        // save the metadata
                        _imageMetadataService.Save(currentImageViewModel.Caption, currentImageViewModel.CaptionAlignment.Value, currentImageViewModel.Font, (Color)currentImageViewModel.Color, currentImageViewModel.Rotation, currentImageViewModel.Filename);

                        // go to the next image
                        bindingSourceImages.MoveNext();
                    }
                }
                else if (MessageBox.Show("This will overwrite the original file.  Are you sure?", "Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    // overwrite the existing image
                    currentImageViewModel.Save(_mainFormViewModel.OutputPath, true);

                    // save the metadata
                    _imageMetadataService.Save(currentImageViewModel.Caption, currentImageViewModel.CaptionAlignment.Value, currentImageViewModel.Font, (Color)currentImageViewModel.Color, currentImageViewModel.Rotation, currentImageViewModel.Filename);

                    // go to the next image
                    bindingSourceImages.MoveNext();
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void SetCaptionAlignment(CaptionAlignments captionAlignment)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking for current image...");
                var currentImageViewModel = bindingSourceImages.Position > -1 ? bindingSourceImages.Current as ImageViewModel : null;

                if (currentImageViewModel != null)
                {
                    _logService.Trace($"Setting caption alignment for \"{currentImageViewModel.Filename}\" to {captionAlignment}...");
                    currentImageViewModel.CaptionAlignment = captionAlignment;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ToolStripButtonDontSave_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();

            try
            {
                // go to the next image
                bindingSourceImages.MoveNext();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void SaveAsToolStripMenuItemSaveAs_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                // save the image
                SaveAs();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void SaveAs()
        {
            _logService.TraceEnter();
            try
            {
                // set the default folder
                folderBrowserDialogSave.SelectedPath = _mainFormViewModel.OutputPath;

                _logService.Trace("Prompting for folder to save to...");
                if (folderBrowserDialogSave.ShowDialog() != DialogResult.OK) return;

                // save the new folder
                _logService.Trace($"Setting default save folder to \"{folderBrowserDialogSave.SelectedPath}\"...");
                _mainFormViewModel.OutputPath = folderBrowserDialogSave.SelectedPath;

                // now save the current image
                _logService.Trace("Saving current image...");
                SaveImage();

                // refresh the display
                bindingSourceMain.ResetBindings(false);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void BindingSourceMain_CurrentChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Watching for property changes to the main model...");
                _mainFormViewModel.PropertyChanged += MainFormViewModel_PropertyChanged;

                // show the zoom
                ShowZoom();

                // show the default output folder
                ShowOutputPath();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void BindingSourceImages_CurrentChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking that there is a current image...");
                if (bindingSourceImages.Position == -1)
                {
                    _logService.Trace("There is no current image.  Exiting...");
                    return;
                }

                _logService.Trace("Getting current image...");
                var imageViewModel = bindingSourceImages.Current as ImageViewModel;

                // set the default values
                if (imageViewModel.CaptionAlignment == null)
                    imageViewModel.CaptionAlignment = _mainFormViewModel.CaptionAlignment;
                else
                    _mainFormViewModel.CaptionAlignment = imageViewModel.CaptionAlignment.Value;

                if (imageViewModel.Color == null)
                    imageViewModel.Color = _mainFormViewModel.Color;
                else
                    _mainFormViewModel.Color = imageViewModel.Color.Value;

                if (imageViewModel.Font == null)
                    imageViewModel.Font = _mainFormViewModel.Font;
                else
                    _mainFormViewModel.Font = imageViewModel.Font;

                // show the picture
                ShowPicture(imageViewModel);

                // show the progress
                ShowProgress();

                // show the rotation
                ShowCaptionAlignment(imageViewModel);

                // show the location
                ShowLocation(imageViewModel);

                // focus in the preview list
                FocusPreviewList();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CheckBoxTopLeft_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                // set the caption alignment to top left
                _logService.Trace("Setting caption alignment to top left...");
                SetCaptionAlignment(CaptionAlignments.TopLeft);
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CheckBoxTopCentre_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                // set the caption alignment to top left
                _logService.Trace("Setting caption alignment to top centre...");
                SetCaptionAlignment(CaptionAlignments.TopCentre);
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CheckBoxTopRight_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Setting caption alignment to top right...");
                SetCaptionAlignment(CaptionAlignments.TopRight);
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ToolStripButtonRotateLeft_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Rotating image 90 degrees left...");
                RotateLeft();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void RotateLeft()
        {
            _logService.TraceEnter();
            try
            {
                // get the current image
                var currentImageViewModel = bindingSourceImages.Current as ImageViewModel;

                switch (currentImageViewModel.Rotation)
                {
                    case Rotations.Ninety:
                        currentImageViewModel.Rotation = Rotations.Zero;
                        break;
                    case Rotations.OneEighty:
                        currentImageViewModel.Rotation = Rotations.Ninety;
                        break;
                    case Rotations.TwoSeventy:
                        currentImageViewModel.Rotation = Rotations.OneEighty;
                        break;
                    case Rotations.Zero:
                        currentImageViewModel.Rotation = Rotations.TwoSeventy;
                        break;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CheckBoxLeft_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Setting caption alignment to middle left...");
                SetCaptionAlignment(CaptionAlignments.MiddleLeft);
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CheckBoxCentre_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Setting caption alignment to middle centre...");
                SetCaptionAlignment(CaptionAlignments.MiddleCentre);
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }

        }

        private void ToolStripButtonRotateRight_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Rotating 90 degrees to the right...");
                RotateRight();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }

        }

        private void RotateRight()
        {
            _logService.TraceEnter();
            try
            {
                var currentImageViewModel = bindingSourceImages.Current as ImageViewModel;

                switch (currentImageViewModel.Rotation)
                {
                    case Rotations.Ninety:
                        currentImageViewModel.Rotation = Rotations.OneEighty;
                        break;
                    case Rotations.OneEighty:
                        currentImageViewModel.Rotation = Rotations.TwoSeventy;
                        break;
                    case Rotations.TwoSeventy:
                        currentImageViewModel.Rotation = Rotations.Zero;
                        break;
                    case Rotations.Zero:
                        currentImageViewModel.Rotation = Rotations.Ninety;
                        break;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void LoadPreviewImages()
        {
            _logService.TraceEnter();
            try
            {
                while (_loadPreviewImageQueue.Count > 0)
                {
                    // get the first image
                    var imageViewModel = _loadPreviewImageQueue[0];

                    // load the image
                    var preview = imageViewModel.LoadPreview();

                    // add it to the image list
                    Invoke(() => {
                        _logService.Trace($"Removing existing preview for \"{imageViewModel.Filename}\"...");
                        imageListLarge.Images.RemoveByKey(imageViewModel.Filename);

                        _logService.Trace($"Adding new preview for \"{imageViewModel.Filename}\"...");
                        imageListLarge.Images.Add(imageViewModel.Filename, preview);
                    });

                    lock (_loadPreviewImageQueue)
                    {
                        // remove the image
                        _loadPreviewImageQueue.RemoveAt(0);
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                Invoke(() => MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error));
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ListViewPreview_SelectedIndexChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                // go to the selected image
                _logService.Trace("Checking if any preview images are selected...");
                if (listViewPreview.SelectedIndices.Count != 1)
                {
                    _logService.Trace("No preview image is selected.  Exiting...");
                    return;
                }

                // get the selected position
                var selectedIndex = listViewPreview.SelectedIndices[0];

                _logService.Trace($"Preview image {selectedIndex} selected.  Updating image position...");
                bindingSourceImages.Position = selectedIndex;
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CheckBoxRight_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Setting caption alignment to middle right...");
                SetCaptionAlignment(CaptionAlignments.MiddleRight);
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CheckBoxBottomLeft_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Setting caption alignment to bottom left...");
                SetCaptionAlignment(CaptionAlignments.BottomLeft);
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                Save();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CheckBoxBottomCentre_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Setting caption alignment to bottom centre...");
                SetCaptionAlignment(CaptionAlignments.BottomCentre);
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CheckBoxBottomLeft_CheckedChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Setting caption alignment to bottom left...");
                SetCaptionAlignment(CaptionAlignments.BottomLeft);
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }

        }

        private void CheckBoxBottomRight_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Setting caption alignment to bottom right...");
                SetCaptionAlignment(CaptionAlignments.BottomRight);
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ToolStripButtonSaveAs_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                SaveAs();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ToolStripComboBoxZoom_Leave(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                ValidateChildren();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void FocusPreviewList()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (InvokeRequired)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoke(new DefaultDelegate(FocusPreviewList));

                    return;
                }
                _logService.Trace("Running on UI thread");

                if (bindingSourceImages.Position == -1)
                {
                    listViewPreview.SelectedIndices.Clear();
                }
                else if (listViewPreview.Items.Count > bindingSourceImages.Position)
                {
                    listViewPreview.Items[bindingSourceImages.Position].Selected = true;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void FontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Changing font...");
                ChangeFont();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ColourToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Changing colour...");
                ChangeColour();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void RotateLeftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Rotating 90 degrees to the left...");
                RotateLeft();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void RotateRightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Rotating right...");
                RotateRight();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ToolStripButtonLocation_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                // get the current image
                var currentImageViewModel = bindingSourceImages.Current as ImageViewModel;

                _logService.Trace($"Opening Google maps at {currentImageViewModel.Latitude.Value},{currentImageViewModel.Longitude.Value}...");
                Process.Start(string.Format(Properties.Settings.Default.MapsURL, currentImageViewModel.Latitude.Value, currentImageViewModel.Longitude.Value));
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}