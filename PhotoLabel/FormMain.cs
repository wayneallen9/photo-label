using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using PhotoLabel.Extensions;
using PhotoLabel.Properties;
using PhotoLabel.Services;

namespace PhotoLabel
{
    public partial class FormMain : Form, IInvoker
    {
        #region constants

        private const string TransparencyOff = "Off";

        #endregion

        public FormMain(
            ILogService logService,
            IPercentageService percentageService,
            FormMainViewModel mainFormViewModel)
        {
            // save dependencies
            _logService = logService;
            _percentageService = percentageService;

            InitializeComponent();

            // initialise the font list
            PopulateFonts();

            // initialise the transparency list
            PopulateTransparency();

            // initialise the view model
            _mainFormViewModel = mainFormViewModel;
            _mainFormViewModel.Invoker = this;

            // add the event handling
            _mainFormViewModel.Error += ErrorHandler;
            _mainFormViewModel.ImageFound += ImageFoundHandler;
            _mainFormViewModel.Opened += OpenedHandler;
            _mainFormViewModel.Opening += OpeningHandler;
            _mainFormViewModel.PreviewLoaded += PreviewLoadedHandler;
            _mainFormViewModel.ProgressChanged += ProgressChangedHandler;
            _mainFormViewModel.PropertyChanged += PropertyChangedHandler;
            _mainFormViewModel.QuickCaption += QuickCaptionHandler;
            _mainFormViewModel.QuickCaptionCleared += QuickCaptionClearedHandler;
            _mainFormViewModel.RecentlyUsedDirectoriesCleared += RecentlyUsedDirectoriesClearedHandler;
            _mainFormViewModel.RecentlyUsedDirectory += RecentlyUsedDirectoryHandler;
        }

        private void ProgressChangedHandler(object sender, ProgressChangedEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                toolStripProgressBarOpen.Maximum = e.Count;
                toolStripProgressBarOpen.Value = e.Current;
                toolStripProgressBarOpen.ToolTipText = e.Directory;
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

        private void RecentlyUsedDirectoryHandler(object sender, RecentlyUsedDirectoryEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Ensuring menu separator is visible...");
                toolStripMenuItemSeparator.Visible = true;

                _logService.Trace($@"Creating menu item ""{e.RecentlyUsedDirectory.Path}""...");
                var menuItem = new ToolStripMenuItem
                {
                    Text = e.RecentlyUsedDirectory.Caption,
                    ToolTipText = e.RecentlyUsedDirectory.Path
                };
                menuItem.Click += RecentlyUsedDirectoryClickHandler;

                _logService.Trace(
                    $"Adding {e.RecentlyUsedDirectory.Path} to recently used directory menu options...");
                toolStripMenuItemFile.DropDownItems.Insert(toolStripMenuItemFile.DropDownItems.Count - 2, menuItem);
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

        private void RecentlyUsedDirectoriesClearedHandler(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Hiding menu separator...");
                toolStripMenuItemSeparator.Visible = false;

                var menuPos = toolStripMenuItemFile.DropDownItems.IndexOf(toolStripMenuItemSeparator) + 1;
                while (toolStripMenuItemFile.DropDownItems[menuPos].ToolTipText != null)
                    toolStripMenuItemFile.DropDownItems.RemoveAt(menuPos);
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

        private void QuickCaptionHandler(object sender, QuickCaptionEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Creating quick caption button for ""{e.Caption}""...");
                var quickCaptionButton = new Button
                {
                    AutoSize = true,
                    Text = e.Caption
                };

                _logService.Trace("Adding event handler for quick caption button...");
                quickCaptionButton.Click += QuickCaptionClickHandler;

                flowLayoutPanelQuickCaption.Controls.Add(quickCaptionButton);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void QuickCaptionClickHandler(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Getting the button that was clicked...");
                var button = (Button) sender;

                _logService.Trace("Setting caption on current image...");
                _mainFormViewModel.Caption = button.Text;
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

        private void QuickCaptionClearedHandler(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Removing all existing quick captions...");
                flowLayoutPanelQuickCaption.Controls.Clear();
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

        private void CheckBoxAppendDateTakenToCaption_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _mainFormViewModel.AppendDateTakenToCaption = checkBoxAppendDateTakenToCaption.Checked;
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

        private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Cancelling any in progress preview loads...");
                _loadVisiblePreviewsCancellationTokenSource?.Cancel();

                _logService.Trace("Disposing view model...");
                _mainFormViewModel.Dispose();
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
                _logService.Trace("Loading recently used directories...");
                _mainFormViewModel.LoadRecentlyUsedDirectories();

                _logService.Trace("Showing append date taken to caption...");
                SetAppendDateTakenToCaption(_mainFormViewModel);

                _logService.Trace($"Showing default output path \"{_mainFormViewModel.OutputPath}\"...");
                ShowOutputPath(_mainFormViewModel);

                _logService.Trace("Setting default window state...");
                SetWindowState(_mainFormViewModel);

                _logService.Trace("Showing default file type...");
                ShowImageFormat(_mainFormViewModel);

                ShowBackgroundSecondColour(_mainFormViewModel);
                ShowBold(_mainFormViewModel);
                ShowFont(_mainFormViewModel);
                ShowSecondColour(_mainFormViewModel);
                ShowTransparency(_mainFormViewModel);
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

        private void ImageFoundHandler(object sender, ImageFoundEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Creating list item for {e.Filename}...");
                var listItem = new ListViewItem
                {
                    ImageIndex = 0,
                    Name = e.Filename,
                    ToolTipText = e.Filename
                };
                listViewPreview.Items.Add(listItem);
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

        private void LoadVisiblePreviews()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Cancelling any previews currently being loaded...");
                _loadVisiblePreviewsCancellationTokenSource?.Cancel();

                _logService.Trace("Creating a new cancellation token source for this load...");
                _loadVisiblePreviewsCancellationTokenSource = new CancellationTokenSource();

                _logService.Trace($"Processing {listViewPreview.Items.Count}...");
                foreach (ListViewItem item in listViewPreview.Items)
                    if (listViewPreview.ClientRectangle.IntersectsWith(item.GetBounds(ItemBoundsPortion.Entire)))
                        _mainFormViewModel.LoadPreview(item.Name, _loadVisiblePreviewsCancellationTokenSource.Token);
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

        private void OpenedHandler(object sender, OpenedEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Hiding opening progress...");
                toolStripProgressBarOpen.Visible = false;

                _logService.Trace("Checking if any images were found...");
                if (e.Count == 0)
                    MessageBox.Show($@"No image files were found in ""{e.Directory}""", @"Open", MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation);
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

        private void OpeningHandler(object sender, OpeningEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Clearing preview list...");
                listViewPreview.Clear();

                _logService.Trace("Showing progress bar...");
                toolStripProgressBarOpen.Value = 0;
                toolStripProgressBarOpen.Visible = true;
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

        private void PopulateTransparency()
        {
            _logService.TraceEnter();
            try
            {
                // add the option to turn the background off
                toolStripComboBoxTransparency.Items.Add("Off");

                // get the format for percentages
                if (!(CultureInfo.CurrentCulture.NumberFormat.Clone() is NumberFormatInfo numberFormatInfo)) return;

                numberFormatInfo.PercentDecimalDigits = 0;

                toolStripComboBoxTransparency.Items.Add(0.25.ToString("P", numberFormatInfo));
                toolStripComboBoxTransparency.Items.Add(0.5.ToString("P", numberFormatInfo));
                toolStripComboBoxTransparency.Items.Add(0.75.ToString("P", numberFormatInfo));
                toolStripComboBoxTransparency.Items.Add(1.ToString("P", numberFormatInfo));
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

                _logService.Trace("Assigning preview to image...");
                var listViewItem = listViewPreview.Items[e.Filename];
                if (listViewItem != null) listViewItem.ImageKey = e.Filename;

                _logService.Trace("Redrawing image list...");
                listViewPreview.Invalidate();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                var formMainViewModel = sender as FormMainViewModel;

                switch (e.PropertyName)
                {
                    case "AppendDateTakenToCaption":
                        SetAppendDateTakenToCaption(formMainViewModel);

                        break;
                    case "Brightness":
                        ShowBrightness(formMainViewModel);

                        break;
                    case "CanDelete":
                        SetFormEnabled(formMainViewModel);

                        break;
                    case "Caption":
                        ShowCaption(formMainViewModel);

                        break;
                    case "CaptionAlignment":
                        ShowCaptionAlignment(formMainViewModel);

                        break;
                    case "Count":
                        ShowProgress(formMainViewModel);

                        break;
                    case "DateTaken":
                        SetAppendDateTakenToCaption(formMainViewModel);

                        break;
                    case "Filename":
                        ShowFilename(formMainViewModel);

                        break;
                    case "FontBold":
                        ShowBold(formMainViewModel);

                        break;
                    case "FontName":
                        ShowFont(formMainViewModel);

                        break;
                    case "FontSize":
                        ShowFont(formMainViewModel);

                        break;
                    case "FontType":
                        ShowFont(formMainViewModel);

                        break;
                    case "Image":
                        ShowImage(formMainViewModel);

                        break;
                    case "ImageFormat":
                        ShowImageFormat(formMainViewModel);

                        break;
                    case "Latitude":
                    case "Longitude":
                        ShowLocation(formMainViewModel);

                        break;
                    case "OutputPath":
                        ShowOutputPath(formMainViewModel);

                        break;
                    case "Position":
                        SetFormEnabled(formMainViewModel);
                        ShowPosition(formMainViewModel);
                        ShowProgress(formMainViewModel);

                        break;
                    case "WindowState":
                        SetWindowState(formMainViewModel);

                        break;
                    default:
                        ShowBackgroundSecondColour(formMainViewModel);
                        ShowSecondColour(formMainViewModel);
                        ShowTransparency(formMainViewModel);
                        SetFormEnabled(formMainViewModel);

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

        private void ShowBrightness(FormMainViewModel formMainViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Synchronising value of track bar...");
                if (trackBarBrightness.Value != formMainViewModel.Brightness)
                    trackBarBrightness.Value = formMainViewModel.Brightness;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void RecentlyUsedDirectoryClickHandler(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Getting clicked menu item...");
                var menuItem = (ToolStripMenuItem) sender;

                _logService.Trace("Checking if the file exists...");
                if (Directory.Exists(menuItem.ToolTipText))
                {
                    _logService.Trace($"Opening \"{menuItem.ToolTipText}\" from recently used file list...");
                    OpenFolder(menuItem.ToolTipText);
                }
                else
                {
                    _logService.Trace($"\"{menuItem.ToolTipText}\" does not exist");
                    if (MessageBox.Show(
                            $@"""{menuItem.ToolTipText}"" could not be found.  Do you wish to remove it from the list of recently used folders?",
                            @"Folder Not Found", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                        DialogResult.Yes)
                        _logService.Trace($"Removing \"{menuItem.ToolTipText}\" from list of recently used files...");
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

        private void SelectImage()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace(
                    $"The current position is {_mainFormViewModel.Position + 1} of {_mainFormViewModel.Count}");

                // synchronise with the position
                if (_mainFormViewModel.Position == -1)
                    while (listViewPreview.SelectedItems.Count > 0)
                        listViewPreview.SelectedItems[0].Selected = false;
                else
                    listViewPreview.Items[_mainFormViewModel.Position].Selected = true;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void SetAppendDateTakenToCaption(FormMainViewModel formMainViewModel)
        {
            var stopWatch = new Stopwatch().StartStopwatch();

            _logService.TraceEnter();
            try
            {
                if (formMainViewModel.DateTaken != null)
                {
                    _logService.Trace("User can append date taken...");
                    checkBoxAppendDateTakenToCaption.Visible = true;

                    _logService.Trace(
                        $"Setting append date taken to caption to {formMainViewModel.AppendDateTakenToCaption}...");
                    checkBoxAppendDateTakenToCaption.Checked = formMainViewModel.AppendDateTakenToCaption;

                    _logService.Trace("Setting append date taken to caption text...");
                    checkBoxAppendDateTakenToCaption.Text = $@"Append {formMainViewModel.DateTaken}?";
                }
                else
                {
                    _logService.Trace("User cannot append date taken...");
                    checkBoxAppendDateTakenToCaption.Visible = false;
                }
            }
            finally
            {
                _logService.TraceExit(stopWatch);
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

        /// <summary>
        ///     Disable or enable toolbar controls depending upon the state of the
        ///     view model.
        /// </summary>
        /// <param name="mainFormViewModel">The view model for the form.</param>
        private void SetFormEnabled(FormMainViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"The current position is {mainFormViewModel.Position}");
                backgroundColourToolStripMenuItem.Enabled =
                colourToolStripMenuItem.Enabled =
                    rotateLeftToolStripMenuItem.Enabled =
                        rotateRightToolStripMenuItem.Enabled =
                            saveToolStripMenuItem.Enabled =
                                saveAsToolStripMenuItem.Enabled =
                                    toolStripButtonBackgroundColour.Enabled =
                                        toolStripComboBoxTransparency.Enabled =
                                            trackBarBrightness.Enabled =
                                                buttonBrightness.Enabled=
                                            mainFormViewModel.Position > -1;
                toolStripButtonBackgroundSecondColour.Enabled =
                    toolStripButtonColour.Enabled =
                        toolStripButtonSecondColour.Enabled =
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

        private void ShowBackgroundSecondColour(FormMainViewModel formMainViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if there is a secondary background colour...");
                if (formMainViewModel.BackgroundSecondColour == null)
                {
                    toolStripButtonBackgroundSecondColour.Visible = false;
                }
                else if (formMainViewModel.BackgroundSecondColour.Value.A == 0)
                {
                    toolStripButtonBackgroundSecondColour.Visible = false;
                }
                else
                {
                    toolStripButtonBackgroundSecondColour.Image = formMainViewModel.BackgroundSecondColourImage;
                    toolStripButtonBackgroundSecondColour.Visible = true;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowBold(FormMainViewModel formMainViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Setting bold value to {formMainViewModel.FontBold}...");
                toolStripButtonBold.Checked = formMainViewModel.FontBold;
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

        private void ShowFont(FormMainViewModel formMainViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Updating font selection to \"{formMainViewModel.FontName}\"...");
                toolStripComboBoxFonts.Text = formMainViewModel.FontName;
                toolStripComboBoxSizes.Text = formMainViewModel.FontSize.ToString(CultureInfo.CurrentCulture);
                toolStripComboBoxType.Text = formMainViewModel.FontType;
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

        private void ShowLocation(FormMainViewModel formMainViewModel)
        {
            _logService.TraceEnter();
            try
            {
                toolStripButtonLocation.Enabled =
                    formMainViewModel.Latitude != null && formMainViewModel.Longitude != null;
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

                _logService.Trace("Resizing progress bar...");
                ResizeProgressBar();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowImage(FormMainViewModel mainFormViewModel)
        {
            _logService.TraceEnter();
            try
            {
                if (mainFormViewModel.Image != null)
                {
                    // has the image changed?
                    if (pictureBoxImage.Image == mainFormViewModel.Image) return;

                    // release the existing image (if it has changed)
                    if (pictureBoxImage.Image != _ajaxImage)
                        // release the image memory
                        pictureBoxImage.Image?.Dispose();

                    // show the image
                    pictureBoxImage.Image = mainFormViewModel.Image;

                    // set the size mode for the image
                    SetSizeMode();
                }
                else
                {
                    ShowAjaxImage();
                }

                // let the image update
                Application.DoEvents();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowPosition(FormMainViewModel formMainViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if there is a selected preview...");
                if (formMainViewModel.Position == -1)
                {
                    _logService.Trace("No preview is selected.  Clearing selected previews and returning...");
                    foreach (ListViewItem selectedListItem in listViewPreview.SelectedItems)
                        selectedListItem.Selected = false;

                    return;
                }

                _logService.Trace($"Getting preview at position {formMainViewModel.Position}...");
                var listItem = listViewPreview.Items[formMainViewModel.Position];

                _logService.Trace("Checking if preview is already selected...");
                if (listItem.Selected)
                {
                    _logService.Trace(
                        $"Preview at position {formMainViewModel.Position} is already selected.  Exiting...");
                    return;
                }

                _logService.Trace($"Selecting preview at position {formMainViewModel.Position}...");
                listItem.Selected = true;

                _logService.Trace($"Ensuring that preview at position {formMainViewModel.Position} is visible...");
                listItem.EnsureVisible();

                LoadVisiblePreviews();

                ShowProgress(formMainViewModel);

                ResizeProgressBar();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ShowProgress(FormMainViewModel formMainViewModel)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Image count is {formMainViewModel.Count}");
                if (formMainViewModel.Count == 0)
                {
                    toolStripStatusLabelStatus.Visible = false;
                }
                else
                {
                    _logService.Trace("Updating status bar progress...");
                    toolStripStatusLabelStatus.Text = $@"{formMainViewModel.Position + 1} of {formMainViewModel.Count}";
                    toolStripStatusLabelStatus.Visible = true;
                }
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

        private void ShowTransparency(FormMainViewModel formMainViewModel)
        {
            _logService.TraceEnter();
            try
            {
                // get the alpha of the background colour
                var alpha = formMainViewModel.BackgroundColour.A;

                if (alpha == 0)
                {
                    toolStripComboBoxTransparency.Text = TransparencyOff;
                }
                else
                {
                    // get the percentage
                    var percentage = _percentageService.ConvertToString(alpha / 255f);

                    toolStripComboBoxTransparency.Text = percentage;
                }
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

        private void ToolStripButtonBackgroundColour_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                ChooseBackgroundColour();
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

        private void ChooseBackgroundColour()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Setting default background color...");
                colorDialog.Color = _mainFormViewModel.BackgroundColour;

                _logService.Trace("Showing color dialog...");
                if (colorDialog.ShowDialog() != DialogResult.OK) return;

                _logService.Trace("Getting transparency...");
                var transparency = toolStripComboBoxTransparency.Text == TransparencyOff
                    ? 0
                    : _percentageService.ConvertToFloat(toolStripComboBoxTransparency.Text);

                _logService.Trace("Setting background color...");
                _mainFormViewModel.BackgroundColour = Color.FromArgb((byte) (transparency * 255), colorDialog.Color);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ToolStripButtonBackgroundSecondColour_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Setting background colour...");
                _mainFormViewModel.UseBackgroundSecondColour();
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

        private void ToolStripButtonDontSave_Click(object sender, EventArgs e)
        {
            var stopWatch = new Stopwatch().StartStopwatch();

            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if the position can be incremented...");
                if (_mainFormViewModel.Position >= _mainFormViewModel.Count - 1)
                {
                    _logService.Trace("Already at last position.  Exiting...");

                    return;
                }

                _logService.Trace("Incrementing position...");
                _mainFormViewModel.Position++;
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Resources.ERROR_TEXT, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _logService.TraceExit(stopWatch);
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

        private void ToolStripButtonSave_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Saving file...");
                Save();
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, Resources.ERROR_CAPTION, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

        private void ToolStripComboBoxSizes_Validating(object sender, CancelEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Validating size of \"{toolStripComboBoxSizes.Text}\"...");
                e.Cancel = !float.TryParse(toolStripComboBoxSizes.Text, out float result);

                _logService.Trace("Checking that value is greater than 0...");
                if (result <= 0) e.Cancel = true;
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

        private void ToolStripComboBoxTransparency_DrawItem(object sender, DrawItemEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                // get the target control
                if (!(sender is ToolStripComboBox comboBox)) return;

                // make sure it is within the range
                if (e.Index <= -1 || e.Index >= comboBox.Items.Count) return;

                // draw the background
                e.DrawBackground();

                if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
                    e.DrawFocusRectangle();

                // get the transparency
                var transparency = comboBox.Items[e.Index].ToString();
                byte alpha = 255;
                if (transparency != "Off")
                {
                    // get the percentage value
                    var transparencyValue = _percentageService.ConvertToFloat(transparency);
                    alpha = (byte) (255 * transparencyValue);
                }

                // create the colour
                var textColour = Color.FromArgb(alpha, e.ForeColor);

                using (var textBrush = new SolidBrush(textColour))
                {
                    // get the name of the font
                    var fontFamilyName = comboBox.Items[e.Index].ToString();

                    // create the font
                    var font = new Font(fontFamilyName, 10, FontStyle.Regular);

                    // draw the font on the control
                    e.Graphics.DrawString(fontFamilyName, font, textBrush, e.Bounds);
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

        private void ToolStripComboBoxTransparency_SelectedIndexChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                switch (toolStripComboBoxTransparency.SelectedIndex)
                {
                    case -1:
                        break;
                    case 0:
                        _mainFormViewModel.BackgroundColour = Color.FromArgb(0, _mainFormViewModel.BackgroundColour);

                        break;
                    default:
                        var percentage = _percentageService.ConvertToFloat(toolStripComboBoxTransparency.Text);

                        _mainFormViewModel.BackgroundColour = Color.FromArgb((byte) (percentage * 255),
                            _mainFormViewModel.BackgroundColour);

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

        private void ToolStripComboBoxTransparency_Validated(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Checking for ""{TransparencyOff}""...");
                if (toolStripComboBoxTransparency.Text == TransparencyOff)
                {
                    _logService.Trace($@"Value is ""{TransparencyOff}"".  Returning...");
                    _mainFormViewModel.BackgroundColour = Color.FromArgb(0, _mainFormViewModel.BackgroundColour);

                    return;
                }

                _logService.Trace($@"Trying to parse ""{toolStripComboBoxTransparency.Text}"" as a percentage...");
                var percentage = _percentageService.ConvertToFloat(toolStripComboBoxTransparency.Text);

                _logService.Trace($@"""{toolStripComboBoxTransparency.Text}"" is a valid percentage");
                toolStripComboBoxTransparency.Text = _percentageService.ConvertToString(percentage);

                _logService.Trace(@"Setting backgrond colour...");
                _mainFormViewModel.BackgroundColour =
                    Color.FromArgb((int) (255 * percentage), _mainFormViewModel.BackgroundColour);
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

        private void ToolStripComboBoxTransparency_Validating(object sender, CancelEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Checking for ""{TransparencyOff}""...");
                if (toolStripComboBoxTransparency.Text == TransparencyOff)
                {
                    _logService.Trace($@"Value is ""{TransparencyOff}"".  Returning...");
                    e.Cancel = false;

                    return;
                }

                _logService.Trace($@"Trying to parse ""{toolStripComboBoxTransparency.Text}"" as a percentage...");
                _percentageService.ConvertToFloat(toolStripComboBoxTransparency.Text);

                _logService.Trace($@"""{toolStripComboBoxTransparency.Text}"" is a valid percentage");
                e.Cancel = false;
            }
            catch (FormatException)
            {
                _logService.Trace($@"""{toolStripComboBoxTransparency.Text}"" is not a valid percentage");
                e.Cancel = true;
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

        #region variables

        private CancellationTokenSource _loadVisiblePreviewsCancellationTokenSource;
        private readonly IPercentageService _percentageService;

        #endregion

        #region delegates

        #endregion

        #region variables

        private readonly Image _ajaxImage = Resources.ajax;
        private readonly ILogService _logService;
        private readonly FormMainViewModel _mainFormViewModel;

        #endregion

        private void StatusStrip_SizeChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                ResizeProgressBar();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ResizeProgressBar()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Resizing progress bar...");
                toolStripProgressBarOpen.Width = statusStrip.Width - toolStripStatusLabelOutputDirectory.Width -
                                                 toolStripStatusLabelStatus.Width - 40;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void BackgroundColourToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                ChooseBackgroundColour();
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

        private void TrackBarBrightness_ValueChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Updating value of brightness to {trackBarBrightness.Value}...");
                _mainFormViewModel.Brightness = trackBarBrightness.Value;
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

        private void ButtonBrightness_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try {
                _logService.Trace("Resetting brightness to 0...");
                _mainFormViewModel.Brightness = 0;

                _logService.Trace($"Setting focus to {nameof(trackBarBrightness)}...");
                trackBarBrightness.Focus();
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