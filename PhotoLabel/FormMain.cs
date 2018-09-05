using PhotoLibrary.Models;
using PhotoLibrary.Services;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace PhotoLabel
{
    public partial class FormMain : Form
    {
        #region variables
        private readonly IImageService _imageService;
        private readonly ILogService _logService;
        private readonly MainFormViewModel _mainFormViewModel;
        #endregion

        public FormMain(IImageService imageService, ILogService logService, MainFormViewModel mainFormViewModel)
        {
            // save dependencies
            _imageService = imageService;
            _logService = logService;
            _mainFormViewModel = mainFormViewModel;

            InitializeComponent();
            InitialiseModel();
        }

        private int? GetZoomValue()
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

                return null;
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

        private void OpenFolder()
        {
            _logService.TraceEnter();
            try
            {
                // show the dialog
                if (folderBrowserDialogImages.ShowDialog() == DialogResult.OK)
                {

                    OpenFolder(folderBrowserDialogImages.SelectedPath);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OpenFolder(string folderPath)
        {
            // set the cursor
            Cursor = Cursors.WaitCursor;

            // let the cursor show
            Application.DoEvents();

            try
            {
                // load the images
                _mainFormViewModel.Images = Directory.GetFiles(folderPath, "*.jpg").Select(filename =>
                {
                    // create the model
                    var model = NinjectKernel.Get<ImageViewModel>();

                    // set the filename
                    model.Filename = filename;

                    return model;
                }).ToList();

                // add it to the list of recently used files
                _mainFormViewModel.RecentlyUsedFiles.Add(folderPath);
                _mainFormViewModel.Save();

                // redraw the images
                bindingSourceMain.ResetBindings(false);
            }
            finally
            {
                // reset the cursor
                Cursor = Cursors.Default;
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
                // set the current font
                fontDialog.Font = _mainFormViewModel.Font;

                // now show the dialog
                if (fontDialog.ShowDialog() == DialogResult.OK)
                {
                    // save the new font
                    _mainFormViewModel.Font = fontDialog.Font;

                    // update the font on the current image
                    if (bindingSourceImages.Position > -1)
                    {
                        ((ImageViewModel)bindingSourceImages.Current).Font = fontDialog.Font;

                        // rebind to the image
                        bindingSourceImages.ResetBindings(false);
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

        private void InitialiseModel()
        {
            _logService.TraceEnter();
            try
            {
                // set the binding source
                bindingSourceMain.DataSource = _mainFormViewModel;

                // update the controls that cannot be bound
                toolStripComboBoxZoom.Text = $"{_mainFormViewModel.Zoom}%";

                // set the recently used files
                foreach (var recentlyUsedFile in _mainFormViewModel.RecentlyUsedFiles)
                {
                    // show the separator
                    toolStripMenuItemSeparator.Visible = true;

                    // create the new menu item
                    var menuItem = new ToolStripMenuItem
                    {
                        Text = recentlyUsedFile
                    };

                    menuItem.Click += (s, e) =>
                    {
                        OpenFolder(recentlyUsedFile);
                    };

                    // add it to the menu
                    toolStripMenuItemFile.DropDownItems.Insert(toolStripMenuItemFile.DropDownItems.Count - 2, menuItem);
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

        private void ToolStripComboBoxZoom_Validated(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                // get the zoom value
                var zoom = GetZoomValue();

                // was it a valid value?
                if (zoom.HasValue)
                {
                    // set the new zoom
                    _mainFormViewModel.Zoom = zoom.Value;
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

        private void BindingSourceImages_CurrentItemChanged(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                // is it properly position?
                if (bindingSourceImages.Position == -1) return;

                // get the currently displayed image
                var imageViewModel = bindingSourceImages.Current as ImageViewModel;

                // do we need to set any defaults?
                if (imageViewModel.Brush == null) imageViewModel.Brush = new SolidBrush(_mainFormViewModel.Color);
                if (imageViewModel.Font == null) imageViewModel.Font = _mainFormViewModel.Font;

                // get the currently displayed image
                if (imageViewModel.Image == null) return;

                // set the size of the image
                pictureBoxImage.Height = imageViewModel.Image.Height * _mainFormViewModel.Zoom / 100;
                pictureBoxImage.Width = imageViewModel.Image.Width * _mainFormViewModel.Zoom / 100;

                // set the status text
                toolStripStatusLabelStatus.Text = $"{bindingSourceImages.Position + 1} of {bindingSourceImages.Count}";
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

        private void ToolStripComboBoxZoom_Validating(object sender, System.ComponentModel.CancelEventArgs e)
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
        }

        private void ToolStripComboBoxZoom_Leave(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                // validate the form when leaving the control
                if (!Validate()) toolStripComboBoxZoom.Focus();
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
                // update the zoom
                var zoom = GetZoomValue();

                if (zoom.HasValue)
                {
                    _mainFormViewModel.Zoom = zoom.Value;

                    // redraw the image
                    bindingSourceImages.ResetBindings(false);
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

        private void ToolStripButtonColour_Click(object sender, EventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                // set the default color
                colorDialog.Color = _mainFormViewModel.Color;

                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    // update the color
                    _mainFormViewModel.Color = colorDialog.Color;

                    // update the colour on any existing image
                    if (bindingSourceImages.Position > -1)
                    {
                        ((ImageViewModel)bindingSourceImages.Current).Brush = new SolidBrush(colorDialog.Color);

                        bindingSourceImages.ResetBindings(false);
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
    }
}