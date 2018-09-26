using PhotoLabel.Services.Models;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
namespace PhotoLabel.Services
{
    public class ImageService : IImageService
    {
        #region variables
        private readonly IImageLoaderService _imageLoaderService;
        private readonly ILineWrapService _lineWrapService;
        private readonly ILogService _logService;
        #endregion

        public ImageService(
            IImageLoaderService imageLoaderService,
            ILineWrapService lineWrapService,
            ILogService logService)
        {
            // save the dependency injections
            _imageLoaderService = imageLoaderService;
            _lineWrapService = lineWrapService;
            _logService = logService;
        }

        public Image Get(string filename)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Getting \"{filename}\"...");
                return _imageLoaderService.Load(filename);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Image Get(string filename, int width, int height)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Getting \"{filename}\"...");
                var image = _imageLoaderService.Load(filename);

                return Resize(image, width, height);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public ExifData GetExifData(string filename)
        {
            _logService.TraceEnter();
            try
            {
                // create the object to return
                var exifData = new ExifData();

                using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var bitmapSource = BitmapFrame.Create(fs);
                    var bitmapMetadata = (BitmapMetadata)bitmapSource.Metadata;

                    // try and parse the date
                    if (!DateTime.TryParse(bitmapMetadata.DateTaken, out DateTime dateTaken))
                        exifData.DateTaken = bitmapMetadata.DateTaken;
                    else
                        exifData.DateTaken = dateTaken.Date.ToShortDateString();

                    // is there a latitude on the image
                    exifData.Latitude = GetLatitude(bitmapMetadata);
                    if (exifData.Latitude.HasValue) exifData.Longitude = GetLongitude(bitmapMetadata);
                }

                return exifData;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private float? GetLatitude(BitmapMetadata bitmapMetadata)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if file has latitude information...");
                if (!(bitmapMetadata.GetQuery("System.GPS.Latitude.Proxy") is string latitudeRef))
                {
                    _logService.Trace("File does not have latitude information.  Exiting...");
                    return null;
                }

                _logService.Trace($"Parsing latitude information \"{latitudeRef}\"...");
                var latitudeMatch = Regex.Match(latitudeRef, @"^(\d+),([0123456789.]+)([SN])");
                if (!latitudeMatch.Success)
                {
                    _logService.Trace($"Unable to parse \"{latitudeRef}\".  Exiting...");
                    return null;
                }

                _logService.Trace("Converting to a float...");
                var latitudeDecimal = float.Parse(latitudeMatch.Groups[1].Value) + float.Parse(latitudeMatch.Groups[2].Value) / 60;
                if (latitudeMatch.Groups[3].Value == "S") latitudeDecimal *= -1;

                return latitudeDecimal;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private float? GetLongitude(BitmapMetadata bitmapMetadata)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if file has longitude information...");
                if (!(bitmapMetadata.GetQuery("System.GPS.Longitude.Proxy") is string longitudeRef))
                {
                    _logService.Trace("File does not have longitude information.  Exiting...");
                    return null;
                }

                _logService.Trace($"Parsing longitude information \"{longitudeRef}\"...");
                var longitudeMatch = Regex.Match(longitudeRef, @"^(\d+),([0123456789.]+)([EW])");
                if (!longitudeMatch.Success)
                {
                    _logService.Trace($"Unable to parse \"{longitudeRef}\".  Exiting...");
                    return null;
                }

                _logService.Trace("Converting to a float...");
                var longitudeDecimal = float.Parse(longitudeMatch.Groups[1].Value) + float.Parse(longitudeMatch.Groups[2].Value) / 60;
                if (longitudeMatch.Groups[3].Value == "W") longitudeDecimal *= -1;

                return longitudeDecimal;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Image Caption(string filename, string caption, CaptionAlignments captionAlignment, Font font, Brush brush, Rotations rotation)
        {
            Bitmap image;

            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Getting \"{filename}\"...");
                var original = _imageLoaderService.Load(filename);

                lock (original)
                {
                    _logService.Trace("Creating a copy of the original image...");
                    image = new Bitmap(original);
                }

                _logService.Trace($"Rotating copy to {rotation}...");
                switch (rotation)
                {
                    case Rotations.Ninety:
                        image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        break;
                    case Rotations.OneEighty:
                        image.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        break;
                    case Rotations.TwoSeventy:
                        image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        break;
                }

                // create a copy of the rotated image (to workaround the problem drawing strings
                // outside the bounds of the unrotated image)
                var rotatedImage = new Bitmap(image);

                _logService.Trace("Getting graphics manager for new image...");
                using (var graphics = Graphics.FromImage(rotatedImage))
                {
                    _logService.Trace("Setting up graphics manager...");
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    _logService.Trace("Determining size of caption...");
                    var captionSize = graphics.MeasureString(caption, font);

                    _logService.Trace("Determining location for caption...");
                    switch (captionAlignment)
                    {
                        case CaptionAlignments.BottomCentre:
                            CaptionBottomCentre(graphics, rotatedImage.Size, caption, font, brush);

                            break;
                        case CaptionAlignments.BottomLeft:
                            // draw them on the image
                            CaptionBottomLeft(graphics, rotatedImage.Size, caption, font, brush);

                            break;
                        case CaptionAlignments.BottomRight:
                            CaptionBottomRight(graphics, rotatedImage.Size, caption, font, brush);

                            break;
                        case CaptionAlignments.MiddleCentre:
                            CaptionMiddleCentre(graphics, rotatedImage.Size, caption, font, brush);

                            break;
                        case CaptionAlignments.MiddleLeft:
                            CaptionMiddleLeft(graphics, rotatedImage.Size, caption, font, brush);

                            break;
                        case CaptionAlignments.MiddleRight:
                            CaptionMiddleRight(graphics, rotatedImage.Size, caption, font, brush);

                            break;
                        case CaptionAlignments.TopCentre:
                            CaptionTopCentre(graphics, rotatedImage.Size, caption, font, brush);

                            break;
                        case CaptionAlignments.TopLeft:
                            CaptionTopLeft(graphics, rotatedImage.Size, caption, font, brush);

                            break;
                        case CaptionAlignments.TopRight:
                            CaptionTopRight(graphics, rotatedImage.Size, caption, font, brush);

                            break;
                    }
                }

                return rotatedImage;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CaptionBottomCentre(Graphics graphics, Size imageSize, string caption, Font font, Brush brush)
        {
            _logService.TraceEnter();
            try
            {
                // load the lines from the bottom
                _logService.Trace($"Wrapping caption to fit on image {imageSize.Width}px wide...");
                var lines = _lineWrapService.WrapToFitFromBottom(graphics, imageSize, caption, font);

                // draw each line from the bottom
                var y = imageSize.Height - 10f;
                for (var lineNumber = lines.Count - 1; lineNumber != -1; lineNumber--)
                {
                    // get this line
                    var line = lines[lineNumber];

                    // if the line is empty, make it a space, so it has some size
                    if (line == string.Empty) line = " ";

                    // get the size of this line
                    var lineSize = graphics.MeasureString(line, font);

                    // workout the location for this line
                    y -= lineSize.Height;
                    var location = new PointF((imageSize.Width - lineSize.Width) / 2, y);
                    graphics.DrawString(line, font, brush, location);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CaptionBottomLeft(Graphics graphics, Size imageSize, string caption, Font font, Brush brush)
        {
            _logService.TraceEnter();
            try
            {
                // get the lines
                _logService.Trace($"Wrapping caption to fit on image {imageSize.Width}px wide...");
                var lines = _lineWrapService.WrapToFitFromBottom(graphics, imageSize, caption, font);

                // draw each line from the bottom
                var y = imageSize.Height - 10f;
                for (var lineNumber = lines.Count - 1; lineNumber != -1; lineNumber--)
                {
                    // get this line
                    var line = lines[lineNumber];

                    // if the line is empty, make it a space, so it has some size
                    if (line == string.Empty) line = " ";

                    // get the size of this line
                    var lineSize = graphics.MeasureString(line, font);

                    // workout the location for this line
                    y -= lineSize.Height;
                    var location = new PointF(10, y);
                    graphics.DrawString(line, font, brush, location);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CaptionBottomRight(Graphics graphics, Size imageSize, string caption, Font font, Brush brush)
        {
            _logService.TraceEnter();
            try
            {
                // get the lines
                _logService.Trace($"Wrapping caption to fit on image {imageSize.Width}px wide...");
                var lines = _lineWrapService.WrapToFitFromBottom(graphics, imageSize, caption, font);

                // draw each line from the bottom
                var y = imageSize.Height - 10f;
                for (var lineNumber = lines.Count - 1; lineNumber != -1; lineNumber--)
                {
                    // get this line
                    var line = lines[lineNumber];

                    // if the line is empty, make it a space, so it has some size
                    if (line == string.Empty) line = " ";

                    // get the size of this line
                    var lineSize = graphics.MeasureString(line, font);

                    // workout the location for this line
                    y -= lineSize.Height;
                    var location = new PointF(imageSize.Width - lineSize.Width - 10, y);
                    graphics.DrawString(line, font, brush, location);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CaptionMiddleCentre(Graphics graphics, Size imageSize, string caption, Font font, Brush brush)
        {
            _logService.TraceEnter();
            try
            {
                // load the lines from the top
                _logService.Trace($"Splitting caption \"{caption}\" into lines that fit on image...");
                var lines = _lineWrapService.WrapToFitFromTop(graphics, imageSize, caption, font);

                // calculate the total line height
                _logService.Trace("Calculating total height for caption...");
                var totalLineHeight = lines.Sum(l => graphics.MeasureString(l, font).Height);

                // work out the starting position
                var y = (imageSize.Height - totalLineHeight) / 2;
                for (var lineNumber = 0; lineNumber < lines.Count; lineNumber++)
                {
                    // get this line
                    var line = lines[lineNumber];

                    // if the line is empty, make it a space, so it has some size
                    if (line == string.Empty) line = " ";

                    // get the size of this line
                    var lineSize = graphics.MeasureString(line, font);

                    // workout the location for this line
                    var location = new PointF((imageSize.Width - lineSize.Width) / 2, y);
                    graphics.DrawString(line, font, brush, location);
                    y += lineSize.Height;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CaptionMiddleLeft(Graphics graphics, Size imageSize, string caption, Font font, Brush brush)
        {
            _logService.TraceEnter();
            try
            {
                // load the lines from the top
                _logService.Trace($"Splitting caption \"{caption}\" into lines that fit on image...");
                var lines = _lineWrapService.WrapToFitFromTop(graphics, imageSize, caption, font);

                // calculate the total line height
                _logService.Trace("Calculating total height for caption...");
                var totalLineHeight = lines.Sum(l => graphics.MeasureString(l, font).Height);

                // work out the starting position
                var y = (imageSize.Height - totalLineHeight) / 2;
                for (var lineNumber = 0; lineNumber < lines.Count; lineNumber++)
                {
                    // get this line
                    var line = lines[lineNumber];

                    // if the line is empty, make it a space, so it has some size
                    if (line == string.Empty) line = " ";

                    // get the size of this line
                    var lineSize = graphics.MeasureString(line, font);

                    // workout the location for this line
                    var location = new PointF(10, y);
                    graphics.DrawString(line, font, brush, location);
                    y += lineSize.Height;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CaptionMiddleRight(Graphics graphics, Size imageSize, string caption, Font font, Brush brush)
        {
            _logService.TraceEnter();
            try
            {
                // load the lines from the top
                _logService.Trace($"Splitting caption \"{caption}\" into lines that fit on image...");
                var lines = _lineWrapService.WrapToFitFromTop(graphics, imageSize, caption, font);

                // calculate the total line height
                _logService.Trace("Calculating total height for caption...");
                var totalLineHeight = lines.Sum(l => graphics.MeasureString(l, font).Height);

                // work out the starting position
                var y = (imageSize.Height - totalLineHeight) / 2;
                for (var lineNumber = 0; lineNumber < lines.Count; lineNumber++)
                {
                    // get this line
                    var line = lines[lineNumber];

                    // if the line is empty, make it a space, so it has some size
                    if (line == string.Empty) line = " ";

                    // get the size of this line
                    var lineSize = graphics.MeasureString(line, font);

                    // workout the location for this line
                    var location = new PointF(imageSize.Width - lineSize.Width - 10, y);
                    graphics.DrawString(line, font, brush, location);
                    y += lineSize.Height;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CaptionTopCentre(Graphics graphics, Size imageSize, string caption, Font font, Brush brush)
        {
            _logService.TraceEnter();
            try
            {
                // load the lines from the top
                _logService.Trace($"Splitting caption \"{caption}\" into lines that fit on image...");
                var lines = _lineWrapService.WrapToFitFromTop(graphics, imageSize, caption, font);

                // work out the starting position
                var y = 10f;
                for (var lineNumber = 0; lineNumber < lines.Count; lineNumber++)
                {
                    // get this line
                    var line = lines[lineNumber];

                    // if the line is empty, make it a space, so it has some size
                    if (line == string.Empty) line = " ";

                    // get the size of this line
                    var lineSize = graphics.MeasureString(line, font);

                    // workout the location for this line
                    var location = new PointF((imageSize.Width - lineSize.Width) / 2, y);
                    graphics.DrawString(line, font, brush, location);
                    y += lineSize.Height;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CaptionTopLeft(Graphics graphics, Size imageSize, string caption, Font font, Brush brush)
        {
            _logService.TraceEnter();
            try
            {
                // load the lines from the top
                _logService.Trace($"Splitting caption \"{caption}\" into lines that fit on image...");
                var lines = _lineWrapService.WrapToFitFromTop(graphics, imageSize, caption, font);

                // work out the starting position
                var y = 10f;
                for (var lineNumber = 0; lineNumber < lines.Count; lineNumber++)
                {
                    // get this line
                    var line = lines[lineNumber];

                    // if the line is empty, make it a space, so it has some size
                    if (line == string.Empty) line = " ";

                    // get the size of this line
                    var lineSize = graphics.MeasureString(line, font);

                    // workout the location for this line
                    var location = new PointF(10, y);
                    graphics.DrawString(line, font, brush, location);
                    y += lineSize.Height;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void CaptionTopRight(Graphics graphics, Size imageSize, string caption, Font font, Brush brush)
        {
            _logService.TraceEnter();
            try
            {
                // load the lines from the top
                _logService.Trace($"Splitting caption \"{caption}\" into lines that fit on image...");
                var lines = _lineWrapService.WrapToFitFromTop(graphics, imageSize, caption, font);

                // work out the starting position
                var y = 10f;
                for (var lineNumber = 0; lineNumber < lines.Count; lineNumber++)
                {
                    // get this line
                    var line = lines[lineNumber];

                    // if the line is empty, make it a space, so it has some size
                    if (line == string.Empty) line = " ";

                    // get the size of this line
                    var lineSize = graphics.MeasureString(line, font);

                    // workout the location for this line
                    var location = new PointF(imageSize.Width - lineSize.Width - 10, y);
                    graphics.DrawString(line, font, brush, location);
                    y += lineSize.Height;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Image Overlay(string filename, int width, int height, Image overlay, int x, int y)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Getting \"{filename}\"...");
                var original = _imageLoaderService.Load(filename);

                _logService.Trace("Creating base image...");
                var baseImage = Resize(original, width, height);

                _logService.Trace("Getting graphics manager for new image...");
                using (var graphics = Graphics.FromImage(baseImage))
                {
                    _logService.Trace("Setting up graphics manager...");
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    _logService.Trace("Drawing overlay...");
                    graphics.DrawImage(overlay, new Point(x, y));
                }

                return baseImage;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private Image Resize(Image image, int width, int height)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Resizing to fit {width}px x{height}px canvas...");
                var canvas = new Bitmap(width, height);

                _logService.Trace($"Calculating size of resized image...");
                var aspectRatio = Math.Min(width / (float)image.Width, height / (float)image.Height);
                var newWidth = image.Width * aspectRatio;
                var newHeight = image.Height * aspectRatio;
                var newX = (width - newWidth) / 2;
                var newY = (height - newHeight) / 2;

                _logService.Trace($"Drawing resized image...");
                using (var graphics = Graphics.FromImage(canvas))
                {
                    // initialise the pen
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    // draw the black background
                    graphics.FillRectangle(new SolidBrush(Color.Black), 0, 0, width, height);

                    // now draw the image over the top
                    graphics.DrawImage(image, newX, newY, newWidth, newHeight);
                }

                return canvas;
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}