using PhotoLibrary.Services;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
namespace PhotoLabel.ViewModels
{
    public class ImageViewModel
    {
        #region variables
        private string _caption;
        private readonly IImageService _imageService;
        private readonly ILogService _logService;
        #endregion

        public ImageViewModel(
            IImageService imageService,
            ILogService logService)
        {
            // save the injections
            _imageService = imageService;
            _logService = logService;
        }

        public Color? Color { get; set; }

        public string Caption {
            get
            {
                // has a caption been set?
                if (_caption != null) return _caption;

                // set the default caption
                _caption = _imageService.GetDateTaken(Filename);

                return _caption;
            }
            set
            {
                // save the new caption
                _caption = value;
            }
        }

        public CaptionAlignments? CaptionAlignment { get; set; }

        public string Filename { get; set; }

        public string FilenameWithoutPath
        {
            get => Path.GetFileName(Filename);
        }

        public Font Font { get; set; }

        /*public Image Image {
            get
            {
                // do we have a cached version of the image?
                if (_image != null) return _image;

                // get the image
                var image = _imageService.Get(Filename);

                // cache the image with the caption
                var brush = new SolidBrush((Color)Color);
                _image = _imageService.Caption(image, Caption, CaptionAlignment.Value, Font, brush, Rotation);

                return _image;
            }
        }*/

        public Rotations Rotation { get; set; } = Rotations.Zero;

        public bool Save(string outputFolder, bool overwriteIfExists)
        {
            _logService.TraceEnter();
            try
            {
                // create the target filename
                var filenameWithoutPath = Path.GetFileName(Filename);
                var targetFilename = Path.Combine(outputFolder, filenameWithoutPath);

                _logService.Trace($"Checking if \"{targetFilename}\" already exists...");
                if (File.Exists(targetFilename))
                {
                    _logService.Trace($"\"{targetFilename}\" already exists");
                    if (!overwriteIfExists) return false;
                }
                _logService.Trace($"\"{targetFilename}\" will be overwritten");

                var original = _imageService.Get(Filename);
                lock (original)
                {
                    // create the image to save
                    var captioned = _imageService.Caption(original, Caption, CaptionAlignment.Value, Font, new SolidBrush(Color.Value), Rotation);

                    // save the image to the file
                    _logService.Trace($"Saving \"{targetFilename}\"...");
                    captioned.Save(targetFilename, ImageFormat.Jpeg);
                }

                // flag that the image has been saved
                Saved = true;

                return true;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public bool Saved { get; set; } = false;
    }
}