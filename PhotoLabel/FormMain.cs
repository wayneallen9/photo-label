using PhotoLabel.ViewModels;
using PhotoLibrary.Services;
using System;
using System.Drawing;
using System.Globalization;
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
        #endregion

        #region variables
        private readonly IImageMetadataService _imageMetadataService;
        private readonly IImageService _imageService;
        private ImageViewModel _imageViewModel;
        private volatile CancellationTokenSource _loadImageCancellationTokenSource;
        private volatile bool _loading = false;
        private readonly ILogService _logService;
        private readonly MainFormViewModel _mainFormViewModel;
        private CancellationTokenSource _openFolderCancellationTokenSource;
        private readonly IRecentlyUsedFilesService _recentlyUsedFilesService;
        #endregion

        public FormMain(
            IImageService imageService,
            ILogService logService,
            MainFormViewModel mainFormViewModel,
            IImageMetadataService imageMetadataService,
            IRecentlyUsedFilesService recentlyUsedFilesService)
        {
            // save dependencies
            _imageMetadataService = imageMetadataService;
            _imageService = imageService;
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

        private int GetZoomValue()
        {
            _logService.TraceEnter();
            try
            {
                // get the current culture
                var currentCulture = CultureInfo.CurrentCulture;

                // get the current number formant
                var numberFormatInfo = currentCulture.NumberFormat;

                // get the percentage sign for that culture
                var percentageSign = numberFormatInfo.PercentSymbol;

                // remove the percentage sign
                var valueAsNumber = toolStripComboBoxZoom.Text.Replace(percentageSign, string.Empty);

                // get the new value
                if (int.TryParse(valueAsNumber, out int value)) return value;

                return 100;
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

        private void LoadImage()
        {
            // if an image load is in progress, cancel it
            if (_loadImageCancellationTokenSource != null) _loadImageCancellationTokenSource.Cancel();

            // load the new image
            _loadImageCancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew((parameters) =>
            {
                LoadImageThread(_imageViewModel.Filename, _imageViewModel.Caption, _imageViewModel.CaptionAlignment.Value, _imageViewModel.Font, _imageViewModel.Color.Value, _imageViewModel.Rotation, _loadImageCancellationTokenSource.Token);
            }, _loadImageCancellationTokenSource.Token, TaskCreationOptions.LongRunning);
        }

        private void LoadImageThread(string filename, string caption, CaptionAlignments captionAlignment, Font font, Color color, Rotations rotation, CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                // load the image
                if (cancellationToken.IsCancellationRequested) return;
                var image = _imageService.Get(filename);
                lock (image)
                {
                    // add the caption
                    if (cancellationToken.IsCancellationRequested) return;
                    var captionedImage = _imageService.Caption(image, caption, captionAlignment, font, new SolidBrush(color), rotation);

                    if (cancellationToken.IsCancellationRequested) return;
                    Invoke(() =>
                    {
                        if (cancellationToken.IsCancellationRequested) return;

                        // set the image
                        pictureBoxImage.Image = captionedImage;

                        // zoom the image
                        ZoomImage();

                        if (cancellationToken.IsCancellationRequested) return;
                        if (pictureBoxImage.Height < panelSize.Height)
                            pictureBoxImage.Top = (panelSize.Height - pictureBoxImage.Height) / 2;
                        else
                            pictureBoxImage.Top = 0;

                        if (pictureBoxImage.Width < panelSize.Width)
                            pictureBoxImage.Left = (panelSize.Width - pictureBoxImage.Width) / 2;
                        else
                            pictureBoxImage.Left = 0;
                    });
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(FromHandle(Handle), Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                _logService.Trace("Checking if there is a current image...");
                if (_imageViewModel == null)
                {
                    _logService.Trace("There is no current image.  Exiting...");
                    return;
                }

                _logService.Trace("Setting zoom text...");
                toolStripComboBoxZoom.Text = string.Format("{0:P0}", _mainFormViewModel.Zoom / 100f);

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

        /// <summary>
        /// Cancels any in progress load then loads all images from the specified path.
        /// </summary>
        /// <param name="folderPath">The source path for the images.</param>
        private void OpenFolder(string folderPath)
        {
            _logService.TraceEnter();
            try
            {
                // flag that we are loading
                _loading = true;

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

                // we have finished loading
                _loading = false;
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
                pictureBoxImage.Left = (panelSize.Width - pictureBoxImage.Width) / 2;
                pictureBoxImage.Top = (panelSize.Height - pictureBoxImage.Height) / 2;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OpenFolderThread(string folderPath, CancellationToken cancellationToken)
        {
            Image previewImage;

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
                        // clear the active image
                        _imageViewModel = null;

                        // rebind to the UI
                        RebindModel();

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

                    // add the preview list items
                    _logService.Trace("Creating preview list items...");
                    foreach (var imageListModel in _mainFormViewModel.Images)
                    {
                        // has the user cancelled the load?
                        if (cancellationToken.IsCancellationRequested) break;

                        Invoke(() =>
                        {
                            listViewPreview.Items.Add(new ListViewItem
                            {
                                ImageKey = imageListModel.Filename,
                                Selected = bindingSourceImages.Position == listViewPreview.Items.Count,
                                ToolTipText = imageListModel.Filename
                            });
                        });

                        // don't overwhelm the UI
                        Thread.Sleep(10);
                    }

                    _logService.Trace("Loading preview images...");
                    foreach (var imageListModel in _mainFormViewModel.Images)
                    {
                        // does this image have metadata?
                        if (cancellationToken.IsCancellationRequested) break;
                        _logService.Trace($"Checking if \"{imageListModel.Filename}\" has metadata...");
                        var hasMetadata = _imageMetadataService.HasMetadata(imageListModel.Filename);

                        // load the preview image
                        if (cancellationToken.IsCancellationRequested) break;
                        if (hasMetadata)
                            previewImage = _imageService.Overlay(imageListModel.Filename, imageListLarge.ImageSize.Width, imageListLarge.ImageSize.Height, Properties.Resources.metadata, imageListLarge.ImageSize.Width - Properties.Resources.metadata.Width - 4, 4);
                        else
                            previewImage = _imageService.Get(imageListModel.Filename, imageListLarge.ImageSize.Width, imageListLarge.ImageSize.Height);

                        // now display it
                        if (cancellationToken.IsCancellationRequested) break;
                        Invoke(() =>
                        {
                            // add the image to the image list
                            if (cancellationToken.IsCancellationRequested) return;
                            imageListLarge.Images.Add(imageListModel.Filename, previewImage);
                        });
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
                // set the current font
                _logService.Trace($"Defaulting to current image font \"{_imageViewModel.Font.Name}\"...");
                fontDialog.Font = _imageViewModel.Font;

                // now show the dialog
                if (fontDialog.ShowDialog() == DialogResult.OK)
                {
                    // save the new font as the default font
                    _logService.Trace("Updating default font...");
                    _mainFormViewModel.Font = fontDialog.Font;

                    // update the font on the current image
                    _logService.Trace("Updating font on current image...");
                    _imageViewModel.Font = fontDialog.Font;

                    // update the display
                    bindingSourceImages.ResetBindings(false);
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
                // get the zoom value
                var zoom = GetZoomValue();

                // set the new zoom
                _mainFormViewModel.Zoom = GetZoomValue();

                // zoom the image
                ZoomImage();
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

        /*private void ToolStripComboBoxZoom_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                // get the current culture
                var currentCulture = CultureInfo.CurrentCulture;

                // get the current number formant
                var numberFormatInfo = currentCulture.NumberFormat;

                // get the percentage sign for that culture
                var percentageSign = numberFormatInfo.PercentSymbol;

                // remove the percentage sign
                var valueAsNumber = toolStripComboBoxZoom.Text.Replace(percentageSign, string.Empty);

                // it must be an integer
                e.Cancel = !int.TryParse(valueAsNumber, out int value);
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
        }*/

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
                // set the default color
                _logService.Trace("Defaulting to current image colour...");
                colorDialog.Color = _imageViewModel.Color.Value;

                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    // update the color
                    _logService.Trace("Updating default colour...");
                    _mainFormViewModel.Color = colorDialog.Color;

                    // update the colour on any existing image
                    _logService.Trace("Updating colour on current image...");
                    _imageViewModel.Color = colorDialog.Color;

                    // now update the screen
                    RebindModel();
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

        private void RebindModel()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Manually binding caption alignment buttons...");
                checkBoxTopLeft.Checked = _imageViewModel?.CaptionAlignment == CaptionAlignments.TopLeft;
                checkBoxTopCentre.Checked = _imageViewModel?.CaptionAlignment == CaptionAlignments.TopCentre;
                checkBoxTopRight.Checked = _imageViewModel?.CaptionAlignment == CaptionAlignments.TopRight;
                checkBoxLeft.Checked = _imageViewModel?.CaptionAlignment == CaptionAlignments.MiddleLeft;
                checkBoxCentre.Checked = _imageViewModel?.CaptionAlignment == CaptionAlignments.MiddleCentre;
                checkBoxRight.Checked = _imageViewModel?.CaptionAlignment == CaptionAlignments.MiddleRight;
                checkBoxBottomLeft.Checked = _imageViewModel?.CaptionAlignment == CaptionAlignments.BottomLeft;
                checkBoxBottomCentre.Checked = _imageViewModel?.CaptionAlignment == CaptionAlignments.BottomCentre;
                checkBoxBottomRight.Checked = _imageViewModel?.CaptionAlignment == CaptionAlignments.BottomRight;

                _logService.Trace("Manually binding default caption alignment...");
                _mainFormViewModel.CaptionAlignment = _imageViewModel?.CaptionAlignment ?? CaptionAlignments.TopLeft;

                _logService.Trace("Manually binding properties that cannot be bound automatically...");
                colourToolStripMenuItem.Enabled = 
                    fontToolStripMenuItem.Enabled = 
                    rotateLeftToolStripMenuItem.Enabled =
                    rotateRightToolStripMenuItem.Enabled =
                    saveToolStripMenuItem.Enabled = 
                    saveAsToolStripMenuItem.Enabled = _imageViewModel != null;
                toolStripButtonColour.Enabled = 
                    toolStripButtonDontSave.Enabled = 
                    toolStripButtonFont.Enabled = 
                    toolStripButtonRotateLeft.Enabled = 
                    toolStripButtonRotateRight.Enabled =
                    toolStripButtonSave.Enabled = 
                    toolStripButtonSaveAs.Enabled = _imageViewModel != null;

                _logService.Trace("Checking if there is a current image...");
                if (_imageViewModel == null)
                {
                    _logService.Trace("There is no current image.");
                    pictureBoxImage.Image = null;

                    _logService.Trace("Clearing status bar position...");
                    toolStripStatusLabelStatus.Text = string.Empty;
                }
                else
                {
                    _logService.Trace("Loading current image...");
                    LoadImage();

                    _logService.Trace("Updating position in status bar...");
                    toolStripStatusLabelStatus.Text = $"{bindingSourceImages.Position + 1} of {bindingSourceImages.Count}";
                }

                toolStripComboBoxZoom.Text = string.Format("{0:P0}", _mainFormViewModel.Zoom / 100d);
                toolStripStatusLabelOutputDirectory.Text = _mainFormViewModel.OutputPath;
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
                } else
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
                // is there a current image?
                _logService.Trace("Checking if there is a current image...");
                if (_imageViewModel == null)
                {
                    _logService.Trace("There is no current image.  Exiting...");
                    return;
                }

                // are we overwriting the original?
                var outputPath = Path.Combine(_mainFormViewModel.OutputPath, _imageViewModel.FilenameWithoutPath);
                _logService.Trace($"Destination file is \"{outputPath}\"");
                if (outputPath != _imageViewModel.Filename)
                {
                    // try and save the image
                    if (_imageViewModel.Save(_mainFormViewModel.OutputPath, false))
                    {
                        // save the metadata
                        _imageMetadataService.Save(_imageViewModel.Caption, _imageViewModel.CaptionAlignment.Value, _imageViewModel.Font, (Color)_imageViewModel.Color, _imageViewModel.Rotation, _imageViewModel.Filename);

                        // show the saved preview image
                        Task.Run(() => ShowSavedPreviewImageThread(_imageViewModel.Filename));

                        // go to the next image
                        bindingSourceImages.MoveNext();
                    }
                    else if (MessageBox.Show($"The file \"{_imageViewModel.FilenameWithoutPath}\" already exists.  Do you wish to overwrite it?", "Overwrite file?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        // overwrite the existing image
                        _imageViewModel.Save(_mainFormViewModel.OutputPath, true);

                        // save the metadata
                        _imageMetadataService.Save(_imageViewModel.Caption, _imageViewModel.CaptionAlignment.Value, _imageViewModel.Font, (Color)_imageViewModel.Color, _imageViewModel.Rotation, _imageViewModel.Filename);

                        // show the saved preview image
                        Task.Run(() => ShowSavedPreviewImageThread(_imageViewModel.Filename));

                        // go to the next image
                        bindingSourceImages.MoveNext();
                    }
                }
                else if (MessageBox.Show("This will overwrite the original file.  Are you sure?", "Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    // overwrite the existing image
                    _imageViewModel.Save(_mainFormViewModel.OutputPath, true);

                    // save the metadata
                    _imageMetadataService.Save(_imageViewModel.Caption, _imageViewModel.CaptionAlignment.Value, _imageViewModel.Font, (Color)_imageViewModel.Color, _imageViewModel.Rotation, _imageViewModel.Filename);

                    // show the saved preview image
                    Task.Run(() => ShowSavedPreviewImageThread(_imageViewModel.Filename));

                    // go to the next image
                    bindingSourceImages.MoveNext();
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowSavedPreviewImageThread(string filename)
        {
            _logService.TraceEnter();
            try
            {
                // update the image
                _logService.Trace($"Replacing the preview image for \"{filename}\"...");
                var newImage = _imageService.Overlay(filename, imageListLarge.ImageSize.Width, imageListLarge.ImageSize.Height, Properties.Resources.saved, imageListLarge.ImageSize.Width - 20, 4);

                Invoke(() =>
                {
                    // replace the old image with the new image
                    imageListLarge.Images.RemoveByKey(filename);
                    imageListLarge.Images.Add(filename, newImage);
                });
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
                if (_imageViewModel != null)
                {
                    _logService.Trace($"Setting caption alignment for \"{_imageViewModel.Filename}\" to {captionAlignment}...");
                    _imageViewModel.CaptionAlignment = captionAlignment;
                }

                bindingSourceImages.ResetBindings(false);
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

        private void BindingSourceImages_CurrentItemChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if a load is in progress...");
                if (_loading)
                {
                    _logService.Trace("A load is in progress.  Exiting...");
                    return;
                }

                _logService.Trace("Binding model values to UI...");
                RebindModel();
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

        private void BindingSourceMain_CurrentItemChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                RebindModel();
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
                _logService.Trace("Checking if a load is in progress...");
                if (_loading)
                {
                    _logService.Trace("A load is in progress.  Exiting...");
                    return;
                }

                _logService.Trace("Checking if there is a current image...");
                _imageViewModel = bindingSourceImages.Current as ImageViewModel;

                // set the default values
                if (string.IsNullOrWhiteSpace(_imageViewModel.Caption))
                    _imageViewModel.Caption = _imageService.GetDateTaken(_imageViewModel.Filename) ?? _imageMetadataService.LoadCaption(_imageViewModel.Filename);
                if (_imageViewModel.CaptionAlignment == null)
                    _imageViewModel.CaptionAlignment = _mainFormViewModel.CaptionAlignment;
                if (_imageViewModel.Color == null)
                    _imageViewModel.Color = _mainFormViewModel.Color;
                if (_imageViewModel.Font == null) _imageViewModel.Font = _mainFormViewModel.Font;

                // update the selected preview image (if it is loaded)
                if (listViewPreview.Items.Count > bindingSourceImages.Position)
                {
                    listViewPreview.Items[bindingSourceImages.Position].Selected = true;
                    listViewPreview.Select();
                }

                // select the caption by default
                textBoxCaption.Focus();
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
                switch (_imageViewModel.Rotation)
                {
                    case Rotations.Ninety:
                        _imageViewModel.Rotation = Rotations.Zero;
                        break;
                    case Rotations.OneEighty:
                        _imageViewModel.Rotation = Rotations.Ninety;
                        break;
                    case Rotations.TwoSeventy:
                        _imageViewModel.Rotation = Rotations.OneEighty;
                        break;
                    case Rotations.Zero:
                        _imageViewModel.Rotation = Rotations.TwoSeventy;
                        break;
                }

                // update the screen
                RebindModel();
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
                switch (_imageViewModel.Rotation)
                {
                    case Rotations.Ninety:
                        _imageViewModel.Rotation = Rotations.OneEighty;
                        break;
                    case Rotations.OneEighty:
                        _imageViewModel.Rotation = Rotations.TwoSeventy;
                        break;
                    case Rotations.TwoSeventy:
                        _imageViewModel.Rotation = Rotations.Zero;
                        break;
                    case Rotations.Zero:
                        _imageViewModel.Rotation = Rotations.Ninety;
                        break;
                }
            }
            finally
            {
                _logService.TraceExit();
            }

            // update the screen
            RebindModel();
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
    }
}