using PhotoLabel.Services;
using PhotoLabel.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace PhotoLabel
{
    public partial class FormMain : Form, IObserver
    {
        #region delegates
        private delegate void ActionDelegate(Action action);
        #endregion

        #region variables
        private readonly Image _ajaxImage = Properties.Resources.ajax;
        private readonly Image _loadingImage = Properties.Resources.loading;
        private readonly ILocaleService _localeService;
        private readonly ILogService _logService;
        private readonly MainFormViewModel _mainFormViewModel;
        private readonly ITimerService _timerService;
        #endregion

        public FormMain(
            ILocaleService localeService,
            ILogService logService,
            MainFormViewModel mainFormViewModel,
            ITimerService timerService)
        {
            // save dependencies
            _localeService = localeService;
            _logService = logService;
            _timerService = timerService;

            InitializeComponent();

            // initialise the font list
            PopulateFonts();

            // initialise the view model
            _mainFormViewModel = mainFormViewModel;

            // add the event handling
            _mainFormViewModel.Subscribe(this);
        }

        /// <summary>
        /// Run an action on the UI thread.
        /// </summary>
        /// <param name="action">The <see cref="Action"/> to be executed.</param>
        private void Invoke(Action action)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on the UI thread...");
                if (InvokeRequired)
                {
                    _logService.Trace("Not running on the UI thread.  Delegating to UI thread...");
                    Invoke(new ActionDelegate(Invoke), action);

                    return;
                }

                _logService.Trace("Running on the UI thread.  Executing action...");
                action();
            }
            catch (ObjectDisposedException)
            {
                // ignore this error - the form has been closed
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        /// <summary>
        /// Prompt the user for a target folder.
        /// </summary>
        private void OpenFolder()
        {
            _logService.TraceEnter();
            try
            {
                // show the dialog
                _logService.Trace("Prompting user for folder to open...");
                if (folderBrowserDialogImages.ShowDialog() != DialogResult.OK)
                {
                    _logService.Trace("User cancelled open dialog.  Exiting...");
                    return;
                }
                
                _logService.Trace($"Folder to open is \"{folderBrowserDialogImages.SelectedPath}\"");
                OpenFolder(folderBrowserDialogImages.SelectedPath);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        /// <summary>
        /// Retrieve images from a directory.
        /// </summary>
        /// <param name="folder">The source path for the images.</param>
        private void OpenFolder(string folder)
        {
            _logService.TraceEnter();
            try
            {
                // open the selected directory
                _logService.Trace($"Opening folder \"{folder}\"...");
                _mainFormViewModel.Open(folder);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        /// <summary>
        /// Disable or enable toolbar controls depending upon the state of the 
        /// view model.
        /// </summary>
        /// <param name="mainFormViewModel">The view model for the form.</param>
        private void SetToolbarStatus(MainFormViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"The current position is {mainFormViewModel.Position}");
                colourToolStripMenuItem.Enabled =
                    rotateLeftToolStripMenuItem.Enabled =
                    rotateRightToolStripMenuItem.Enabled =
                    saveToolStripMenuItem.Enabled =
                    saveAsToolStripMenuItem.Enabled = mainFormViewModel.Position > -1;
                toolStripButtonColour.Enabled =
                    toolStripButtonDontSave.Enabled =
                    toolStripButtonRotateLeft.Enabled =
                    toolStripButtonRotateRight.Enabled =
                    toolStripButtonSave.Enabled =
                    toolStripButtonSaveAs.Enabled = mainFormViewModel.Position > -1;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        /// <summary>
        /// Add the available fonts to the drop down list of fonts
        /// </summary>
        private void PopulateFonts()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Adding fonts to combo box list...");
                toolStripComboBoxFonts.Items.AddRange(FontFamily.Families.Select(f => f.Name).ToArray());
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void SetWindowState(MainFormViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                WindowState = mainFormViewModel.WindowState;
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
                // release the existing image
                if (pictureBoxImage.Image != _ajaxImage)
                {
                    _logService.Trace("Releasing existing image...");
                    pictureBoxImage.Image?.Dispose();

                    // show the Ajax icon
                    _logService.Trace("Displaying ajax resource...");
                    pictureBoxImage.Image = _ajaxImage;
                    pictureBoxImage.SizeMode = PictureBoxSizeMode.CenterImage;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowFilename(MainFormViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Current image path is \"{mainFormViewModel.Filename}\"");
                labelFilename.Text = mainFormViewModel.Filename;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowFont(MainFormViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Updating font selection to \"{mainFormViewModel.FontName}\"...");
                toolStripComboBoxFonts.Text = mainFormViewModel.FontName;
                toolStripComboBoxSizes.Text = mainFormViewModel.FontSize.ToString();
                toolStripComboBoxType.Text = mainFormViewModel.FontType;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowLocation(MainFormViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                toolStripButtonLocation.Enabled = mainFormViewModel.Latitude != null && mainFormViewModel.Longitude != null;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowOutputPath(MainFormViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                if (string.IsNullOrWhiteSpace(mainFormViewModel.OutputPath))
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

        private void ShowPicture(MainFormViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                if (mainFormViewModel.Image != null)
                {
                    // has the image changed?
                    if (pictureBoxImage.Image != mainFormViewModel.Image)
                    {
                        // release the existing image (if it has changed)
                        if (pictureBoxImage.Image != _ajaxImage)
                            // release the image memory
                            pictureBoxImage.Image?.Dispose();

                        // show the image
                        pictureBoxImage.Image = mainFormViewModel.Image;

                        // set the size mode for the image
                        SetSizeMode();
                    }
                }
                else if (listViewPreview.Items.Count > 0)
                {
                    ShowAjaxImage();
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void SetSizeMode()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if there is a current image...");
                if (pictureBoxImage.Image == null)
                {
                    _logService.Trace("There is no current image.  Exiting...");
                    return;
                }

                // which size mode should we use?
                if (pictureBoxImage.Image.Width < pictureBoxImage.Width &&
                    pictureBoxImage.Image.Height < pictureBoxImage.Height)
                    pictureBoxImage.SizeMode = PictureBoxSizeMode.CenterImage;
                else
                    pictureBoxImage.SizeMode = PictureBoxSizeMode.Zoom;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowProgress(MainFormViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Image count is {mainFormViewModel.Count}");
                if (mainFormViewModel.Count == 0)
                    toolStripStatusLabelStatus.Visible = false;
                else
                {
                    _logService.Trace("Updating status bar progress...");
                    toolStripStatusLabelStatus.Text = $"{mainFormViewModel.Position + 1} of {mainFormViewModel.Count}";
                    toolStripStatusLabelStatus.Visible = true;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowBold(MainFormViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Setting bold value to {mainFormViewModel.FontBold}...");
                toolStripButtonBold.Checked = mainFormViewModel.FontBold;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowCaption(MainFormViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                // if it is unchanged, don't do anything
                if (textBoxCaption.Text == mainFormViewModel.Caption) return;

                // save the update
                textBoxCaption.Text = mainFormViewModel.Caption;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowCaptionAlignment(MainFormViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                checkBoxTopLeft.Checked = mainFormViewModel.CaptionAlignment == CaptionAlignments.TopLeft;
                checkBoxTopCentre.Checked = mainFormViewModel.CaptionAlignment == CaptionAlignments.TopCentre;
                checkBoxTopRight.Checked = mainFormViewModel.CaptionAlignment == CaptionAlignments.TopRight;
                checkBoxLeft.Checked = mainFormViewModel.CaptionAlignment == CaptionAlignments.MiddleLeft;
                checkBoxCentre.Checked = mainFormViewModel.CaptionAlignment == CaptionAlignments.MiddleCentre;
                checkBoxRight.Checked = mainFormViewModel.CaptionAlignment == CaptionAlignments.MiddleRight;
                checkBoxBottomLeft.Checked = mainFormViewModel.CaptionAlignment == CaptionAlignments.BottomLeft;
                checkBoxBottomCentre.Checked = mainFormViewModel.CaptionAlignment == CaptionAlignments.BottomCentre;
                checkBoxBottomRight.Checked = mainFormViewModel.CaptionAlignment == CaptionAlignments.BottomRight;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowSecondColour(MainFormViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Releasing existing second colour image...");
                //toolStripButtonSecondColour.Image?.Dispose();

                _logService.Trace("Checking if a second colour is set...");
                if (mainFormViewModel.SecondColour == null)
                {
                    _logService.Trace("Second colour is not set.  Hiding button...");
                    toolStripButtonSecondColour.Visible = false;
                }
                else
                {
                    toolStripButtonSecondColour.Image = _mainFormViewModel.SecondColourImage;
                    toolStripButtonSecondColour.Visible = true;
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

        private void ExceptionHandler(Task task)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (InvokeRequired)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoke(() => ExceptionHandler(task));

                    return;
                }

                // log the error message
                _logService.Error(task.Exception);

                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void ShowRecentlyUsedDirectories(MainFormViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                // are there any recently used directories?
                if (mainFormViewModel.Directories.Count == 0)
                {
                    _logService.Trace("Removing existing recently used directory menu options...");

                    // reset and remove any already existing options
                    toolStripMenuItemSeparator.Visible = false;

                    var menuPos = toolStripMenuItemFile.DropDownItems.IndexOf(toolStripMenuItemSeparator) + 1;
                    while (toolStripMenuItemFile.DropDownItems[menuPos].ToolTipText != null)
                        toolStripMenuItemFile.DropDownItems.RemoveAt(menuPos);
                }
                else
                {
                    // set the recently used files
                    _logService.Trace($"Adding {mainFormViewModel.Directories.Count} recently used directory menu options...");
                    var menuPos = toolStripMenuItemFile.DropDownItems.IndexOf(toolStripMenuItemSeparator) + 1;
                    foreach (var recentlyUsedDirectory in mainFormViewModel.Directories)
                    {
                        // show the separator
                        toolStripMenuItemSeparator.Visible = true;

                        // is this a recently used directory menu option?
                        if (toolStripMenuItemFile.DropDownItems[menuPos].ToolTipText != recentlyUsedDirectory.Path)
                        {
                            // create the item to insert
                            var menuItem = new ToolStripMenuItem
                            {
                                Text = recentlyUsedDirectory.Caption,
                                ToolTipText = recentlyUsedDirectory.Path
                            };
                            menuItem.Click += (sender, e) => RecentlyUsedFile_Open(recentlyUsedDirectory.Path);

                            // now insert it
                            toolStripMenuItemFile.DropDownItems.Insert(menuPos, menuItem);
                        }

                        // go to the next menu position
                        menuPos++;
                    }

                    // clear any leftovers
                    while (toolStripMenuItemFile.DropDownItems[menuPos].ToolTipText != null)
                        toolStripMenuItemFile.DropDownItems.RemoveAt(menuPos);
                }
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
                colorDialog.Color = _mainFormViewModel.Colour;

                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    _logService.Trace("Updating colour...");
                    _mainFormViewModel.Colour = colorDialog.Color;
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

                // load any new preview images
                LoadVisiblePreviews();

                // size the image properly
                SetSizeMode();
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
                        //_recentlyUsedFilesService.Remove(filename);
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
                // get the destination path
                var outputPath = Path.Combine(_mainFormViewModel.OutputPath, Path.GetFileNameWithoutExtension(_mainFormViewModel.Filename) + ".jpg");
                _logService.Trace($"Destination file is \"{outputPath}\"");

                if (outputPath == _mainFormViewModel.Filename &&
                    MessageBox.Show("Do you wish to overwrite the original image?", "Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    return;

                if (File.Exists(outputPath) &&
                    MessageBox.Show($"The file \"{outputPath}\" already exists.  Do you wish to overwrite it?", "Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    return;

                Cursor = Cursors.WaitCursor;
                try
                {
                    Application.DoEvents();

                    // save the image
                    _mainFormViewModel.Save(outputPath);
                }
                finally
                {
                    Cursor = Cursors.Default;
                }

                // go to the next image
                SelectImage();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void SelectImage()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"The current position is {_mainFormViewModel.Position + 1} of {_mainFormViewModel.Count}");

                // if there is no currently selected list view item, select the default
                _logService.Trace($"{listViewPreview.SelectedIndices.Count} item(s) selected");
                if (listViewPreview.SelectedIndices.Count == 0)
                {
                    // is there a last selected file?
                    var lastSelectedFilename = _mainFormViewModel.Directories[0].Filename;
                    if (!string.IsNullOrWhiteSpace(lastSelectedFilename)) {
                        var listViewItems = listViewPreview.Items.Find(lastSelectedFilename, true);
                        if (listViewItems.Length > 0)
                            listViewItems[0].Selected = true;
                        else
                            listViewPreview.Items[0].Selected = true;
                    }
                    else
                    {
                        listViewPreview.Items[0].Selected = true;
                    }
                }
                else
                {
                    // get the selected index
                    var selectedIndex = listViewPreview.SelectedIndices[0];

                    // if this is not the last image, go to the next image
                    if (selectedIndex < listViewPreview.Items.Count - 1)
                        listViewPreview.Items[selectedIndex + 1].Selected = true;
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
                _logService.Trace($"Setting caption alignment for \"{_mainFormViewModel.Filename}\" to {captionAlignment}...");
                _mainFormViewModel.CaptionAlignment = captionAlignment;
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
                SelectImage();
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
                switch (_mainFormViewModel.Rotation)
                {
                    case Rotations.Ninety:
                        _mainFormViewModel.Rotation = Rotations.Zero;
                        break;
                    case Rotations.OneEighty:
                        _mainFormViewModel.Rotation = Rotations.Ninety;
                        break;
                    case Rotations.TwoSeventy:
                        _mainFormViewModel.Rotation = Rotations.OneEighty;
                        break;
                    case Rotations.Zero:
                        _mainFormViewModel.Rotation = Rotations.TwoSeventy;
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
                switch (_mainFormViewModel.Rotation)
                {
                    case Rotations.Ninety:
                        _mainFormViewModel.Rotation = Rotations.OneEighty;
                        break;
                    case Rotations.OneEighty:
                        _mainFormViewModel.Rotation = Rotations.TwoSeventy;
                        break;
                    case Rotations.TwoSeventy:
                        _mainFormViewModel.Rotation = Rotations.Zero;
                        break;
                    case Rotations.Zero:
                        _mainFormViewModel.Rotation = Rotations.Ninety;
                        break;
                }
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
                _mainFormViewModel.Position = selectedIndex;

                // make sure the selected preview is visible
                listViewPreview.Items[selectedIndex].EnsureVisible();
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
                    Invoke(FocusPreviewList);

                    return;
                }
                _logService.Trace("Running on UI thread");

                if (_mainFormViewModel.Position == -1)
                {
                    listViewPreview.SelectedIndices.Clear();
                }
                else if (listViewPreview.Items.Count > _mainFormViewModel.Position &&
                    !listViewPreview.Items[_mainFormViewModel.Position].Selected)
                {
                    listViewPreview.Items[_mainFormViewModel.Position].Selected = true;
                }
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
                _logService.Trace($"Opening Google maps at {_mainFormViewModel.Latitude.Value},{_mainFormViewModel.Longitude.Value}...");
                Process.Start(string.Format(Properties.Settings.Default.MapsURL, _mainFormViewModel.Latitude.Value, _mainFormViewModel.Longitude.Value));
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

        private void ToolStripButtonSecondColour_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                // use the secondary colour as the primary colour
                _mainFormViewModel.Colour = _mainFormViewModel.SecondColour.Value;
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

        public void OnOpen(IList<string> filenames)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (InvokeRequired)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoke(() => OnOpen(filenames));

                    return;
                }

                Cursor = Cursors.WaitCursor;
                try
                {
                    Application.DoEvents();

                    _logService.Trace($"Populating list with {filenames.Count} images...");
                    imageListLarge.Images.Clear();
                    imageListLarge.Images.Add(_loadingImage);
                    listViewPreview.Clear();

                    if (filenames.Count > 0)
                    {
                        foreach (var filename in filenames)
                        {
                            listViewPreview.Items.Add(new ListViewItem
                            {
                                ImageIndex = 0,
                                Name=filename,
                                ToolTipText = filename
                            });
                        }
                    }
                    else
                    {
                        // hide the icon
                        pictureBoxImage.Image = null;

                        // let the user know what has happened
                        MessageBox.Show("No image files found.", "Open Directory", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }

                    // load the first set of visible images
                    LoadVisiblePreviews();

                    // select the first item
                    SelectImage();
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void OnUpdate(MainFormViewModel value)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (InvokeRequired)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoke(() => OnUpdate(value));

                    return;
                }

                // perform the updates
                ShowBold(value);
                ShowCaption(value);
                ShowFilename(value);
                ShowFont(value);
                SetWindowState(value);
                ShowCaptionAlignment(value);
                ShowLocation(value);
                ShowOutputPath(value);
                ShowPicture(value);
                ShowProgress(value);
                ShowRecentlyUsedDirectories(value);
                ShowSecondColour(value);
                SetToolbarStatus(value);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void OnError(Exception error)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (InvokeRequired)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoke(() => OnError(error));

                    return;
                }

                // write the error
                _logService.Error(error);

                _logService.Trace("Advising user that an exception has occurred...");
                MessageBox.Show(Properties.Resources.ERROR_TEXT, Properties.Resources.ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void OnPreview(string filename, Image image)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (InvokeRequired)
                {
                    // prevent the UI from locking up
                    _timerService.Pause(TimeSpan.FromMilliseconds(250));

                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoke(() => OnPreview(filename, image));

                    return;
                }

                _logService.Trace("Updating preview image...");
                imageListLarge.Images.RemoveByKey(filename);
                imageListLarge.Images.Add(filename, image);

                _logService.Trace("Redrawing image list...");
                listViewPreview.Invalidate();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void LoadVisiblePreviews()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Processing {listViewPreview.Items.Count}...");
                foreach (ListViewItem item in listViewPreview.Items)
                {
                    if (listViewPreview.ClientRectangle.IntersectsWith(item.GetBounds(ItemBoundsPortion.Entire)))
                    {
                        // has this preview already been loaded?
                        if (imageListLarge.Images.ContainsKey(item.Name)) continue;

                        // add a placeholder for this image
                        imageListLarge.Images.Add(item.Name, _loadingImage);

                        // point the item to the placeholder
                        item.ImageKey = item.Name;

                        // and load the preview
                        _mainFormViewModel.LoadPreview(item.Name);
                    }
                }
            }
            catch (ArgumentException)
            {
                // ignore this error - the form is closing
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ListViewPreview_Scroll(object sender, ScrollEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                // only do it when the scroll has finished
                if (e.Type == ScrollEventType.LargeDecrement ||
                    e.Type == ScrollEventType.LargeIncrement ||
                    e.Type == ScrollEventType.SmallDecrement ||
                    e.Type == ScrollEventType.SmallIncrement ||
                    e.Type == ScrollEventType.EndScroll) LoadVisiblePreviews();
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

        private void TextBoxCaption_TextChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _mainFormViewModel.Caption = textBoxCaption.Text;
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

        private void ToolStripComboBoxFonts_DrawItem(object sender, DrawItemEventArgs e)
        {
            // get the target control
            var comboBox = sender as ToolStripComboBox;

            if (e.Index > -1 && e.Index < comboBox.Items.Count)
            {
                e.DrawBackground();

                if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
                    e.DrawFocusRectangle();

                using (SolidBrush textBrush = new SolidBrush(e.ForeColor))
                {
                    string fontFamilyName;

                    // get the name of the font
                    fontFamilyName = comboBox.Items[e.Index].ToString();

                    // create the font
                    var font = new Font(fontFamilyName, 10, FontStyle.Regular);

                    // draw the font on the control
                    e.Graphics.DrawString(fontFamilyName, font, textBrush, e.Bounds);
                }
            }
        }

        private void ToolStripComboBoxFonts_SelectedIndexChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"User has selected font \"{toolStripComboBoxFonts.Text}\"...");
                _mainFormViewModel.FontName = toolStripComboBoxFonts.Text;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ToolStripComboBoxSizes_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Validating size of \"{toolStripComboBoxSizes.Text}\"...");
                e.Cancel = !float.TryParse(toolStripComboBoxSizes.Text, out float result);
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

        private void ToolStripComboBoxSizes_Validated(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Saving size of \"{toolStripComboBoxSizes.Text}\"...");
                _mainFormViewModel.FontSize = float.Parse(toolStripComboBoxSizes.Text);
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

        private void ToolStripComboBoxFonts_Validated(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Saving font \"{toolStripComboBoxSizes.Text}\"...");
                _mainFormViewModel.FontName = toolStripComboBoxFonts.Text;
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

        private void ToolStripComboBoxType_Validated(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Saving font type \"{toolStripComboBoxType.Text}\"...");
                _mainFormViewModel.FontType = toolStripComboBoxType.Text;
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

        private void ToolStripComboBoxFonts_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Saving font \"{toolStripComboBoxFonts.Text}\"...");
                _mainFormViewModel.FontName = toolStripComboBoxFonts.Text;
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

        private void ToolStripComboBoxSizes_SelectedIndexChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                if (toolStripComboBoxSizes.SelectedIndex > -1)
                {
                    _logService.Trace($"Saving font size \"{toolStripComboBoxSizes.Text}\"...");
                    _mainFormViewModel.FontSize = float.Parse(toolStripComboBoxSizes.Text);
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

        private void ToolStripComboBoxType_SelectedIndexChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                if (toolStripComboBoxType.SelectedIndex > -1)
                {
                    _logService.Trace($"Saving font type \"{toolStripComboBoxType.Text}\"...");
                    _mainFormViewModel.FontType = toolStripComboBoxType.Text;
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

        private void ToolStripButtonBold_CheckedChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Setting bold value to {toolStripButtonBold.Checked}...");
                _mainFormViewModel.FontBold = toolStripButtonBold.Checked;
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