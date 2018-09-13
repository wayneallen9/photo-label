using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
namespace PhotoLibrary.Services
{
    public class ImageService : IImageService
    {
        #region variables
        private string _filenameCache;
        private Image _imageCache;
        private readonly ILineWrapService _lineWrapService;
        private readonly ILogService _logService;
        #endregion

        public ImageService(
            ILineWrapService lineWrapService,
            ILogService logService)
        {
            _lineWrapService = lineWrapService;
            _logService = logService;
        }

        public Image Get(string filename)
        {
            _logService.TraceEnter();
            try
            {
                // has this file been cached previously?
                _logService.Trace($"Checking if \"{filename}\" is already cached...");
                if (filename != _filenameCache)
                {
                    _logService.Trace($"File \"{filename}\" needs to be loaded from disk");

                    // load the file from disk and make a copy of it so that the
                    // file can be closed
                    using (var imageFromFile = Image.FromFile(filename))
                    {
                        // cache the image
                        _imageCache = new Bitmap(imageFromFile);
                    }

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

        public Image Get(string filename, int maxWidth, int maxHeight)
        {
            Image original;

            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Checking if \"{filename}\" is already cached...");
                if (_filenameCache == filename)
                {
                    _logService.Trace($"\"{filename}\" is already cached");
                    original = _imageCache;
                }
                else
                {
                    _logService.Trace($"\"{filename}\" is not cached.  Retrieving from disk...");
                    original = Get(filename);
                }

                _logService.Trace($"Resizing to fit {maxWidth}px x{maxHeight}px canvas...");
                var canvas = new Bitmap(maxWidth, maxHeight);

                _logService.Trace($"Calculating size of resized image...");
                var aspectRatio = Math.Min(maxWidth / (float)original.Width, maxHeight / (float)original.Height);
                var newWidth = original.Width * aspectRatio;
                var newHeight = original.Height * aspectRatio;
                var newX = (maxWidth - newWidth) / 2;
                var newY = (maxHeight - newHeight) / 2;

                _logService.Trace($"Drawing resized image...");
                using (var graphics = Graphics.FromImage(canvas))
                {
                    // initialise the pen
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    // draw the black background
                    graphics.FillRectangle(new SolidBrush(Color.Black), 0, 0, maxWidth, maxHeight);

                    // now draw the image over the top
                    graphics.DrawImage(original, newX, newY, newWidth, newHeight);
                }

                return canvas;
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

        public Image Caption(Image original, string caption, CaptionAlignments captionAlignment, Font font, Brush brush, Rotations rotation)
        {
            List<string> lines;

            _logService.TraceEnter();
            try
            {
                _logService.Trace("Creating a copy of the original image...");
                var image = new Bitmap(original);

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
                _logService.Trace("Creating base image...");
                var baseImage = Get(filename, width, height);

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
    }
}