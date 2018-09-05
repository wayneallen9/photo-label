using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Media.Imaging;
namespace PhotoLibrary.Services
{
    public class ImageService : IImageService
    {
        #region variables
        private string _filenameCache;
        private Image _imageCache;
        private readonly ILogService _logService;
        #endregion

        public ImageService(ILogService logService)
        {
            _logService = logService;
        }

        public Image Get(string filename)
        {
            _logService.TraceEnter();
            try
            {
                // has this file been cached previously?
                _logService.Trace($"Checking if \"{filename}\" is already cached...");
                if (filename != _filenameCache) {
                    _logService.Trace($"File \"{filename}\" needs to be loaded from disk");

                    // cache the image
                    _imageCache = Image.FromFile(filename);

                    // cache the filename
                    _filenameCache = filename;
                }

                return _imageCache;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public string GetDateTaken(string filename)
        {
            _logService.TraceEnter();
            try
            {
                using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    BitmapSource img = BitmapFrame.Create(fs);
                    BitmapMetadata md = (BitmapMetadata)img.Metadata;

                    // try and parse the date
                    if (!DateTime.TryParse(md.DateTaken, out DateTime dateTaken)) return md.DateTaken;

                    return dateTaken.Date.ToShortDateString();
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Image Caption(Image original, string caption, Font font, Brush brush, Point location)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Creating a copy of the original image...");
                var image = new Bitmap(original);

                _logService.Trace("Getting graphics manager for new image...");
                using (var graphics = Graphics.FromImage(image))
                {
                    _logService.Trace("Setting up graphics manager...");
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    _logService.Trace($"Writing \"{caption}\" on image...");
                    graphics.DrawString(caption, font, brush, location);
                }

                return image;
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}