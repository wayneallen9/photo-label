using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PhotoLabel.Properties;
using PhotoLabel.Services;

namespace PhotoLabel
{
    public partial class FormMain : Form, IInvoker
    {
        public FormMain(
            ILogService logService,
            FormMainViewModel mainFormViewModel)
        {
            // save dependencies
            _logService = logService;

            InitializeComponent();

            // initialise the font list
            PopulateFonts();

            // initialise the view model
            _mainFormViewModel = mainFormViewModel;
            _mainFormViewModel.Invoker = this;

            // add the event handling
            _mainFormViewModel.Error += ErrorHandler;
            _mainFormViewModel.PreviewLoaded += PreviewLoadedHandler;
            _mainFormViewModel.PropertyChanged += PropertyChangedHandler;
        }

        private void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                var formMainViewModel = sender as FormMainViewModel;

                switch (e.PropertyName)
                {
                    case "Filenames":
                        PopulatePreviewImages(formMainViewModel);

                        break;
                    case "OutputPath":
                        ShowOutputPath(formMainViewModel);

                        break;
                    case "RecentlyUsedDirectories":
                        ShowRecentlyUsedDirectories(formMainViewModel);

                        break;
                    case "WindowState":
                        SetWindowState(formMainViewModel);

                        break;
                    default:
                        SetToolbarStatus(formMainViewModel);
                        ShowBold(formMainViewModel);
                        ShowCaption(formMainViewModel);
                        ShowFilename(formMainViewModel);
                        ShowFont(formMainViewModel);
                        ShowImageFormat(formMainViewModel);
                        ShowCaptionAlignment(formMainViewModel);
                        ShowLocation(formMainViewModel);
                        ShowPicture(formMainViewModel);
                        ShowProgress(formMainViewModel);
                        ShowSecondColour(formMainViewModel);
                        SetToolbarStatus(formMainViewModel);

                        break;
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ErrorHandler(object sender, ErrorEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Error(e.GetException());

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception)
            {
                // ignore any exceptions whilst reporting an exception
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        /// <summary>
        ///     Prompt the user for a target folder.
        /// </summary>
        private void OpenFolder()
        {
            _logService.TraceEnter();
            try
            {
                // show the dialogue
                _logService.Trace("Prompting user for folder to open...");
                if (folderBrowserDialogImages.ShowDialog() != DialogResult.OK)
                {
                    _logService.Trace("User cancelled open dialogue.  Exiting...");
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
        ///     Retrieve images from a directory.
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
        ///     Disable or enable toolbar controls depending upon the state of the
        ///     view model.
        /// </summary>
        /// <param name="mainFormViewModel">The view model for the form.</param>
        private void SetToolbarStatus(FormMainViewModel mainFormViewModel)
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

                toolStripButtonDelete.Enabled = mainFormViewModel.CanDelete;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        /// <summary>
        ///     Add the available fonts to the drop down list of fonts
        /// </summary>
        private void PopulateFonts()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Adding fonts to combo box list...");
                toolStripComboBoxFonts.Items.AddRange(FontFamily.Families.Select(f => f.Name).ToArray<object>());
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void SetWindowState(FormMainViewModel mainFormViewModel)
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

        private void ShowFilename(FormMainViewModel mainFormViewModel)
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

        private void ShowFont(FormMainViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Updating font selection to \"{mainFormViewModel.FontName}\"...");
                toolStripComboBoxFonts.Text = mainFormViewModel.FontName;
                toolStripComboBoxSizes.Text = mainFormViewModel.FontSize.ToString(CultureInfo.CurrentCulture);
                toolStripComboBoxType.Text = mainFormViewModel.FontType;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowLocation(FormMainViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                toolStripButtonLocation.Enabled =
                    mainFormViewModel.Latitude != null && mainFormViewModel.Longitude != null;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowOutputPath(FormMainViewModel mainFormViewModel)
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

        private void ShowPicture(FormMainViewModel mainFormViewModel)
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

        private void ShowProgress(FormMainViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Image count is {mainFormViewModel.Count}");
                if (mainFormViewModel.Count == 0)
                {
                    toolStripStatusLabelStatus.Visible = false;
                }
                else
                {
                    _logService.Trace("Updating status bar progress...");
                    toolStripStatusLabelStatus.Text = $@"{mainFormViewModel.Position + 1} of {mainFormViewModel.Count}";
                    toolStripStatusLabelStatus.Visible = true;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowBold(FormMainViewModel mainFormViewModel)
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

        private void ShowCaption(FormMainViewModel mainFormViewModel)
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

        private void ShowCaptionAlignment(FormMainViewModel mainFormViewModel)
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

        private void ShowSecondColour(FormMainViewModel mainFormViewModel)
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowRecentlyUsedDirectories(FormMainViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                // are there any recently used directories?
                if (mainFormViewModel.RecentlyUsedDirectories.Count == 0)
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
                    _logService.Trace(
                        $"Adding {mainFormViewModel.RecentlyUsedDirectories.Count} recently used directory menu options...");
                    var menuPos = toolStripMenuItemFile.DropDownItems.IndexOf(toolStripMenuItemSeparator) + 1;
                    foreach (var recentlyUsedDirectory in mainFormViewModel.RecentlyUsedDirectories)
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                // set the default colour
                _logService.Trace("Defaulting to current image colour...");
                colorDialog.Color = _mainFormViewModel.Colour;

                // did the user click ok?
                if (colorDialog.ShowDialog() != DialogResult.OK) return;

                _logService.Trace("Updating colour...");
                _mainFormViewModel.Colour = colorDialog.Color;
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                _logService.Trace("Checking if the file exists...");
                if (Directory.Exists(filename))
                {
                    _logService.Trace($"Opening \"{filename}\" from recently used file list...");
                    OpenFolder(filename);
                }
                else
                {
                    _logService.Trace($"\"{filename}\" does not exist");
                    if (MessageBox.Show(
                            $@"""{filename}"" could not be found.  Do you wish to remove it from the list of recently used folders?",
                            @"Folder Not Found", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                        DialogResult.Yes)
                        _logService.Trace($"Removing \"{filename}\" from list of recently used files...");
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                    if (folderBrowserDialogSave.ShowDialog() != DialogResult.OK) return;

                    // save the save path
                    _mainFormViewModel.OutputPath = folderBrowserDialogSave.SelectedPath;

                    // save the current image
                    SaveImage();
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        /// <summary>
        /// </summary>
        private void SaveImage()
        {
            _logService.TraceEnter();
            try
            {
                // get the destination path
                var outputPath = _mainFormViewModel.OutputFilename;
                _logService.Trace($"Destination file is \"{outputPath}\"");

                if (File.Exists(outputPath) &&
                    MessageBox.Show($@"The file ""{outputPath}"" already exists.  Do you wish to overwrite it?",
                        @"Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
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
                _logService.Trace(
                    $"The current position is {_mainFormViewModel.Position + 1} of {_mainFormViewModel.Count}");

                // if there is no currently selected list view item, select the default
                _logService.Trace($"{listViewPreview.SelectedIndices.Count} item(s) selected");
                if (listViewPreview.SelectedIndices.Count == 0)
                {
                    // is there a last selected file?
                    var lastSelectedFilename = _mainFormViewModel.RecentlyUsedDirectories[0].Filename;
                    if (!string.IsNullOrWhiteSpace(lastSelectedFilename))
                    {
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

        private void ShowImageFormat(FormMainViewModel formMainViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Image format is {formMainViewModel.ImageFormat}");
                if (formMainViewModel.ImageFormat == ImageFormat.Jpeg)
                {
                    toolStripComboBoxImageType.SelectedIndex = 0;
                    toolStripMenuItemJpg.Checked = true;
                    toolStripMenuItemPng.Checked = false;
                }
                else
                {
                    toolStripComboBoxImageType.SelectedIndex = 1;
                    toolStripMenuItemJpg.Checked = false;
                    toolStripMenuItemPng.Checked = true;
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
                _logService.Trace(
                    $"Setting caption alignment for \"{_mainFormViewModel.Filename}\" to {captionAlignment}...");
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                // the latitude and longitude cannot be null
                if (_mainFormViewModel.Latitude == null || _mainFormViewModel.Longitude == null) return;

                _logService.Trace(
                    $"Opening Google maps at {_mainFormViewModel.Latitude.Value},{_mainFormViewModel.Longitude.Value}...");
                Process.Start(string.Format(Settings.Default.MapsURL, _mainFormViewModel.Latitude.Value,
                    _mainFormViewModel.Longitude.Value));
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                if (_mainFormViewModel.SecondColour != null)
                    _mainFormViewModel.Colour = _mainFormViewModel.SecondColour.Value;
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void PopulatePreviewImages(FormMainViewModel formMainViewModel)
        {
            _logService.TraceEnter();
            try
            {
                Cursor = Cursors.WaitCursor;
                try
                {
                    Application.DoEvents();

                    var filenames = formMainViewModel.Filenames;

                    _logService.Trace($"Populating list with {filenames.Count} images...");
                    imageListLarge.Images.Clear();
                    imageListLarge.Images.Add(_loadingImage);
                    listViewPreview.Clear();

                    if (formMainViewModel.Filenames.Count > 0)
                    {
                        foreach (var filename in filenames)
                            listViewPreview.Items.Add(new ListViewItem
                            {
                                ImageIndex = 0,
                                Name = filename,
                                ToolTipText = filename
                            });
                    }
                    else
                    {
                        // hide the icon
                        pictureBoxImage.Image = null;

                        // let the user know what has happened
                        MessageBox.Show(@"No image files found.", @"Open Directory", MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation);
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

        public void PreviewLoadedHandler(object sender, PreviewLoadedEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Updating preview image...");
                imageListLarge.Images.RemoveByKey(e.Filename);
                imageListLarge.Images.Add(e.Filename, e.Image);

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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ToolStripComboBoxFonts_DrawItem(object sender, DrawItemEventArgs e)
        {
            // get the target control
            if (!(sender is ToolStripComboBox comboBox)) return;

            // make sure it is within the range
            if (e.Index <= -1 || e.Index >= comboBox.Items.Count) return;


            e.DrawBackground();

            if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
                e.DrawFocusRectangle();

            using (var textBrush = new SolidBrush(e.ForeColor))
            {
                // get the name of the font
                var fontFamilyName = comboBox.Items[e.Index].ToString();

                // create the font
                var font = new Font(fontFamilyName, 10, FontStyle.Regular);

                // draw the font on the control
                e.Graphics.DrawString(fontFamilyName, font, textBrush, e.Bounds);
            }
        }

        private void ToolStripComboBoxSizes_Validating(object sender, CancelEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Validating size of \"{toolStripComboBoxSizes.Text}\"...");
                e.Cancel = !float.TryParse(toolStripComboBoxSizes.Text, out _);
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ToolStripButtonDelete_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Deleting current image...");
                _mainFormViewModel.Delete();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Showing default output path \"{_mainFormViewModel.OutputPath}\"...");
                ShowOutputPath(_mainFormViewModel);

                _logService.Trace("Showing list of recently used directories...");
                ShowRecentlyUsedDirectories(_mainFormViewModel);

                _logService.Trace("Setting default window state...");
                SetWindowState(_mainFormViewModel);

                _logService.Trace("Showing default file type...");
                ShowImageFormat(_mainFormViewModel);

                ShowBold(_mainFormViewModel);
                ShowFont(_mainFormViewModel);
                ShowSecondColour(_mainFormViewModel);
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        #region delegates

        #endregion

        #region variables

        private readonly Image _ajaxImage = Resources.ajax;
        private readonly Image _loadingImage = Resources.loading;
        private readonly ILogService _logService;
        private readonly FormMainViewModel _mainFormViewModel;

        #endregion

        private void ToolStripComboBoxImageType_SelectedIndexChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Updating image type...");
                _mainFormViewModel.ImageFormat =
                    toolStripComboBoxImageType.SelectedIndex == 0 ? ImageFormat.Jpeg : ImageFormat.Png;
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }


        private void ToolStripMenuItemJpg_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Setting image type to Jpeg...");
                _mainFormViewModel.ImageFormat = ImageFormat.Jpeg;
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ToolStripMenuItemPng_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Setting image type to Png...");
                _mainFormViewModel.ImageFormat = ImageFormat.Png;
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}