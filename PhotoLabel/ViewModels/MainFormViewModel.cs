using PhotoLibrary.Services;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace PhotoLabel.ViewModels
{
    public class MainFormViewModel
    {
        #region variables
        private readonly IImageMetadataService _imageMetadataService;
        private readonly IImageService _imageService;
        private readonly ILogService _logService;
        #endregion

        public MainFormViewModel(
            IImageMetadataService imageMetadataService,
            IImageService imageService,
            ILogService logService)
        {
            // save the dependency injections
            _imageMetadataService = imageMetadataService;
            _imageService = imageService;
            _logService = logService;
        }

        public CaptionAlignments CaptionAlignment
        {
            get => Properties.Settings.Default.CaptionAlignment;
            set
            {
                // update the value
                Properties.Settings.Default.CaptionAlignment = value;

                // persist the changes
                Properties.Settings.Default.Save();

            }
        }
        public Color Color
        {
            get
            {
                try
                {
                    return Properties.Settings.Default.Color;
                }
                catch (NullReferenceException)
                {
                    return Color.White;
                }
            }
            set
            {
                // save the color
                Properties.Settings.Default.Color = value;

                // persist the changes
                Properties.Settings.Default.Save();
            }
        }

        public Font Font {
            get => Properties.Settings.Default.Font ?? SystemFonts.DefaultFont;
            set {
                // save the new font
                Properties.Settings.Default.Font = value;

                // persit the change
                Properties.Settings.Default.Save();
            }
        }

        public BindingList<ImageViewModel> Images { get; set; }

        public int Open(string folderPath)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Loading image files from \"{folderPath}\" and it's subfolders...");
                var images = Directory.EnumerateFiles(folderPath, "*.*")
                    .Where(s => 
                        (
                            s.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase) ||
                            s.EndsWith("*.jpeg", StringComparison.CurrentCultureIgnoreCase) ||
                            s.EndsWith("*.gif", StringComparison.CurrentCultureIgnoreCase) ||
                            s.EndsWith("*.bmp", StringComparison.CurrentCultureIgnoreCase) ||
                            s.EndsWith("*.png", StringComparison.CurrentCultureIgnoreCase)
                        ) &&
                        (File.GetAttributes(s) & FileAttributes.Hidden) == 0)
                    .Select(s =>
                    {
                        // get the image
                        var imageViewModel = NinjectKernel.Get<ImageViewModel>();

                        // try and load it's metadata
                        imageViewModel.Caption = _imageMetadataService.LoadCaption(s);
                        imageViewModel.CaptionAlignment = _imageMetadataService.LoadCaptionAlignment(s);
                        imageViewModel.Color = _imageMetadataService.LoadColor(s);
                        imageViewModel.Font = _imageMetadataService.LoadFont(s);
                        imageViewModel.Rotation = _imageMetadataService.LoadRotation(s) ?? Rotations.Zero;

                        // save the filename
                        imageViewModel.Filename = s;

                        return imageViewModel;
                    }).ToList();

                Images = new BindingList<ImageViewModel>(images);

                return Images.Count;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public string OutputPath
        {
            get => Properties.Settings.Default.OutputPath;
            set
            {
                // save the new value
                Properties.Settings.Default.OutputPath = value;

                // persist the change
                Properties.Settings.Default.Save();
            }
        }

        /*public bool Save(Image original, string caption, CaptionAlignments captionAlignment, Font font, Color color, string filename, bool overwriteIfExists)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Getting output filename...");

                // does the file already exist?
                var outputFilename = Path.Combine(OutputPath, Path.GetFileName(filename));
                _logService.Trace($"Output filename is \"{outputFilename}\"");

                _logService.Trace($"Checking if \"{outputFilename}\" exists...");
                if (File.Exists(outputFilename))
                {
                    _logService.Trace($"\"{outputFilename}\" already exists");
                    if (!overwriteIfExists) return false;
                }

                _logService.Trace("Creating image...");
                var image = _imageService.Caption(original, caption, captionAlignment, font, new SolidBrush(color));

                _logService.Trace($"Writing image to \"{outputFilename}\"...");
                image.Save(outputFilename);

                _logService.Trace($"Writing metadata for \"{outputFilename}\"...");
                _imageMetadataService.Save(caption, captionAlignment, font, color, r, filename);

                return true;
            }
            finally
            {
                _logService.TraceExit();
            }
        }*/

        public FormWindowState WindowState {
            get => Properties.Settings.Default.WindowState;
            set {
                // update the settings
                Properties.Settings.Default.WindowState = value;

                // persist the change
                Properties.Settings.Default.Save();
            }
        }

        public int Zoom {
            get => Properties.Settings.Default.Zoom > 0 ? Properties.Settings.Default.Zoom :  100;
            set {
                // save the new value
                Properties.Settings.Default.Zoom = value;

                // persist the changes
                Properties.Settings.Default.Save();
            }
        }
    }
}