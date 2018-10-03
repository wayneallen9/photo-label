using PhotoLabel.Services;
using PhotoLabel.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
        private readonly ILocaleService _localeService;
        private readonly ILogService _logService;
        private readonly MainFormViewModel _mainFormViewModel;
        private readonly IRecentlyUsedFilesService _recentlyUsedFilesService;
        private readonly ITimerService _timerService;
        #endregion

        public FormMain(
            ILocaleService localeService,
            ILogService logService,
            MainFormViewModel mainFormViewModel,
            IRecentlyUsedFilesService recentlyUsedFilesService,
            ITimerService timerService)
        {
            // save dependencies
            _localeService = localeService;
            _logService = logService;
            _recentlyUsedFilesService = recentlyUsedFilesService;
            _timerService = timerService;

            InitializeComponent();

            // initialise the model binding
            bindingSourceMain.DataSource = _mainFormViewModel = mainFormViewModel;

            // add the event handling
            _mainFormViewModel.Subscribe(this);

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
        /// <param name="directory">The source path for the images.</param>
        private void OpenFolder(string directory)
        {
            _logService.TraceEnter();
            try
            {
                // open the selected directory
                _mainFormViewModel.Open(directory);

                // show the Ajaz icon whilst the directory opens
                ShowAjaxImage();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void SetToolbarStatus(MainFormViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                colourToolStripMenuItem.Enabled =
                    fontToolStripMenuItem.Enabled =
                    rotateLeftToolStripMenuItem.Enabled =
                    rotateRightToolStripMenuItem.Enabled =
                    saveToolStripMenuItem.Enabled =
                    saveAsToolStripMenuItem.Enabled = mainFormViewModel.Position > -1;
                toolStripButtonColour.Enabled =
                    toolStripButtonDontSave.Enabled =
                    toolStripButtonFont.Enabled =
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
                    // show the image
                    pictureBoxImage.Image = mainFormViewModel.Image;

                    // zoom the image
                    ZoomImage();

                    // position it in the form
                    CentrePictureBox();
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

        private void ShowCaption()
        {
            _logService.TraceEnter();
            try
            {
                bindingSourceMain.ResetBindings(false);
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
                _logService.Trace("Checking if a second colour is set...");
                if (mainFormViewModel.SecondColour == null)
                {
                    _logService.Trace("Second colour is not set.  Hiding button...");
                    toolStripButtonSecondColour.Visible = false;
                }
                else
                {
                    _logService.Trace($"Second colour is {mainFormViewModel.SecondColour.Value.ToArgb()}");
                    toolStripButtonSecondColour.BackColor = mainFormViewModel.SecondColour.Value;
                    toolStripButtonSecondColour.Visible = true;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowZoom(MainFormViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Updating zoom to {mainFormViewModel.Zoom}%...");
                toolStripComboBoxZoom.Text = string.Format("{0:P0}", mainFormViewModel.Zoom / 100f);
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
                _logService.Trace($"Defaulting to font \"{_mainFormViewModel.Font.Name}\"...");
                fontDialog.Font = _mainFormViewModel.Font;

                // now show the dialog
                if (fontDialog.ShowDialog() == DialogResult.OK)
                {
                    // save the new font as the default font
                    _logService.Trace("Updating font...");
                    _mainFormViewModel.Font = fontDialog.Font;
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

                    // move this file to the top of the recently used files
                    _recentlyUsedFilesService.Open(filename);

                    DrawRecentlyUsedFiles();
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
                SelectNextImage();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void SelectNextImage()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"The current position is {_mainFormViewModel.Position + 1} of {_mainFormViewModel.Count}");

                // go to the next image
                if (_mainFormViewModel.Position < listViewPreview.Items.Count - 1)
                    listViewPreview.Items[_mainFormViewModel.Position + 1].Selected = true;
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
                SelectNextImage();
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
                    Invoke(() => FocusPreviewList());

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
                    listViewPreview.Clear();

                    for (var i = 0; i < filenames.Count; i++)
                    {
                        imageListLarge.Images.Add(filenames[i], Properties.Resources.loading);
                        listViewPreview.Items.Add(new ListViewItem
                        {
                            ImageKey = filenames[i],
                            ToolTipText = filenames[i]
                        });
                    }

                    // select the first item
                    SelectNextImage();
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
                SetWindowState(value);
                ShowCaption();
                ShowCaptionAlignment(value);
                ShowLocation(value);
                ShowOutputPath(value);
                ShowPicture(value);
                ShowProgress(value);
                ShowSecondColour(value);
                SetToolbarStatus(value);
                ShowZoom(value);
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
                    _timerService.Pause(TimeSpan.FromMilliseconds(500));

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
    }
}