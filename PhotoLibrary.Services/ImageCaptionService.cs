using Shared;
using Shared.Attributes;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading;

namespace PhotoLabel.Services
{
    [Singleton]
    public class ImageCaptionService : IImageCaptionService
    {
        #region variables
        private readonly ILineWrapService _lineWrapService;
        private readonly ILogger _logger;
        #endregion

        public ImageCaptionService(
            ILineWrapService lineWrapService,
            ILogger logger)
        {
            // save dependencies
            _lineWrapService = lineWrapService;
            _logger = logger;
        }

        public Bitmap Caption(Bitmap original, string caption, bool? appendDateTakenToCaption, string dateTaken,
            Rotations? rotation, CaptionAlignments? captionAlignment, string fontName, float fontSize, string fontType,
            bool fontBold, Brush brush, Color backgroundColour, CancellationToken cancellationToken)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Populating defaults...");
                var populatedAppendDateTakenToCaption = appendDateTakenToCaption ?? false;
                var populatedCaptionAlignment = captionAlignment ?? CaptionAlignments.BottomRight;
                var populatedRotation = rotation ?? Rotations.Zero;

                if (cancellationToken.IsCancellationRequested) return null;
                logger.Trace("Building caption...");
                var captionBuilder = new StringBuilder(caption);
                if (populatedAppendDateTakenToCaption && !string.IsNullOrWhiteSpace(dateTaken))
                {
                    if (!string.IsNullOrWhiteSpace(caption)) captionBuilder.Append(" - ");
                    captionBuilder.Append(dateTaken);
                }
                var captionWithDate = captionBuilder.ToString();

                logger.Trace($"Rotating to {populatedRotation}...");
                switch (populatedRotation)
                {
                    case Rotations.Ninety:
                        original.RotateFlip(RotateFlipType.Rotate90FlipNone);

                        break;
                    case Rotations.OneEighty:
                        original.RotateFlip(RotateFlipType.Rotate180FlipNone);

                        break;
                    case Rotations.TwoSeventy:
                        original.RotateFlip(RotateFlipType.Rotate270FlipNone);

                        break;
                }

                logger.Trace("Creating a duplicate of the original image...");
                var duplicate = new Bitmap(original);

                logger.Trace("Checking if there is a caption to render...");
                if (string.IsNullOrWhiteSpace(captionWithDate))
                {
                    logger.Trace("There is not caption to render.  Returning...");
                    return duplicate;
                }

                // work out the style of the font
                if (cancellationToken.IsCancellationRequested) return null;
                var fontStyle = fontBold ? FontStyle.Bold : FontStyle.Regular;

                if (cancellationToken.IsCancellationRequested) return null;
                logger.Trace("Getting graphics manager for new image...");
                using (var graphics = Graphics.FromImage(duplicate))
                {
                    logger.Trace("Setting up graphics manager...");
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    // how is the caption sized?
                    float fontSizeInPoints;
                    if (fontType == "pts")
                    {
                        // use the value provided by the user
                        fontSizeInPoints = fontSize;
                    }
                    else
                    {
                        // work out the logical height of the image (allowing for padding)
                        var height = duplicate.Height - 8;

                        // work out the maximum height for the caption
                        var captionHeight = height * fontSize / 100;

                        // calculate the font size
                        fontSizeInPoints = GetFontSize(graphics, fontName, fontStyle, captionWithDate, captionHeight);
                    }

                    if (cancellationToken.IsCancellationRequested) return null;
                    logger.Trace("Creating font...");
                    using (var font = new Font(fontName, fontSizeInPoints, fontStyle))
                    {
                        if (cancellationToken.IsCancellationRequested) return null;
                        logger.Trace("Determining location for caption...");
                        switch (populatedCaptionAlignment)
                        {
                            case CaptionAlignments.BottomCentre:
                                CaptionBottomCentre(graphics, duplicate.Size, captionWithDate, font, brush,
                                    backgroundColour);

                                break;
                            case CaptionAlignments.BottomLeft:
                                // draw them on the image
                                CaptionBottomLeft(graphics, duplicate.Size, captionWithDate, font, brush,
                                    backgroundColour);

                                break;
                            case CaptionAlignments.BottomRight:
                                CaptionBottomRight(graphics, duplicate.Size, captionWithDate, font, brush,
                                    backgroundColour);

                                break;
                            case CaptionAlignments.MiddleCentre:
                                CaptionMiddleCentre(graphics, duplicate.Size, captionWithDate, font, brush,
                                    backgroundColour);

                                break;
                            case CaptionAlignments.MiddleLeft:
                                CaptionMiddleLeft(graphics, duplicate.Size, captionWithDate, font, brush,
                                    backgroundColour);

                                break;
                            case CaptionAlignments.MiddleRight:
                                CaptionMiddleRight(graphics, duplicate.Size, captionWithDate, font, brush,
                                    backgroundColour);

                                break;
                            case CaptionAlignments.TopCentre:
                                CaptionTopCentre(graphics, duplicate.Size, captionWithDate, font, brush,
                                    backgroundColour);

                                break;
                            case CaptionAlignments.TopLeft:
                                CaptionTopLeft(graphics, duplicate.Size, captionWithDate, font, brush,
                                    backgroundColour);

                                break;
                            case CaptionAlignments.TopRight:
                                CaptionTopRight(graphics, duplicate.Size, captionWithDate, font, brush,
                                    backgroundColour);

                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(captionAlignment),
                                    captionAlignment,
                                    null);
                        }
                    }
                }

                return duplicate;
            }
        }

        private void CaptionBottomCentre(Graphics graphics, Size imageSize, string caption, Font font, Brush brush, Color backgroundColour)
        {
            using (var logger = _logger.Block())
            {
                // load the lines from the bottom
                logger.Trace($"Wrapping caption to fit on image {imageSize.Width}px wide...");
                var lines = _lineWrapService.WrapToFitFromBottom(graphics, imageSize, caption, font);

                // draw each line from the bottom
                float y = imageSize.Height;
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

                    // draw the background
                    graphics.FillRectangle(new SolidBrush(backgroundColour), location.X, location.Y, lineSize.Width, lineSize.Height);

                    // draw the caption
                    graphics.DrawString(line, font, brush, location);
                }

            }
        }

        private void CaptionBottomLeft(Graphics graphics, Size imageSize, string caption, Font font, Brush brush, Color backgroundColour)
        {
            using (var logger = _logger.Block())
            {
                // get the lines
                logger.Trace($"Wrapping caption to fit on image {imageSize.Width}px wide...");
                var lines = _lineWrapService.WrapToFitFromBottom(graphics, imageSize, caption, font);

                // draw each line from the bottom
                float y = imageSize.Height;
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
                    var location = new PointF(0, y);

                    // draw the background
                    graphics.FillRectangle(new SolidBrush(backgroundColour), location.X, location.Y, lineSize.Width, lineSize.Height);

                    // draw the caption
                    graphics.DrawString(line, font, brush, location);
                }

            }
        }

        private void CaptionBottomRight(Graphics graphics, Size imageSize, string caption, Font font, Brush brush, Color backgroundColour)
        {
            using (var logger = _logger.Block())
            {
                // get the lines
                logger.Trace($"Wrapping caption to fit on image {imageSize.Width}px wide...");
                var lines = _lineWrapService.WrapToFitFromBottom(graphics, imageSize, caption, font);

                // draw each line from the bottom
                float y = imageSize.Height;
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
                    var location = new PointF(imageSize.Width - lineSize.Width, y);

                    // draw the background
                    graphics.FillRectangle(new SolidBrush(backgroundColour), location.X, location.Y, lineSize.Width, lineSize.Height);

                    // draw the caption
                    graphics.DrawString(line, font, brush, location);
                }

            }
        }

        private void CaptionMiddleCentre(Graphics graphics, Size imageSize, string caption, Font font, Brush brush, Color backgroundColour)
        {
            using (var logger = _logger.Block())
            {
                // load the lines from the top
                logger.Trace($"Splitting caption \"{caption}\" into lines that fit on image...");
                var lines = _lineWrapService.WrapToFitFromTop(graphics, imageSize, caption, font);

                // calculate the total line height
                logger.Trace("Calculating total height for caption...");
                var totalLineHeight = lines.Sum(l => graphics.MeasureString(l, font).Height);

                // work out the starting position
                var y = (imageSize.Height - totalLineHeight) / 2;
                foreach (var iterator in lines)
                {
                    // get this line
                    var line = iterator;

                    // if the line is empty, make it a space, so it has some size
                    if (line == string.Empty) line = " ";

                    // get the size of this line
                    var lineSize = graphics.MeasureString(line, font);

                    // workout the location for this line
                    var location = new PointF((imageSize.Width - lineSize.Width) / 2, y);

                    // draw the background
                    graphics.FillRectangle(new SolidBrush(backgroundColour), location.X, location.Y, lineSize.Width, lineSize.Height);

                    // draw the caption
                    graphics.DrawString(line, font, brush, location);

                    y += lineSize.Height;
                }

            }
        }

        private void CaptionMiddleLeft(Graphics graphics, Size imageSize, string caption, Font font, Brush brush, Color backgroundColour)
        {
            using (var logger = _logger.Block())
            {
                // load the lines from the top
                logger.Trace($"Splitting caption \"{caption}\" into lines that fit on image...");
                var lines = _lineWrapService.WrapToFitFromTop(graphics, imageSize, caption, font);

                // calculate the total line height
                logger.Trace("Calculating total height for caption...");
                var totalLineHeight = lines.Sum(l => graphics.MeasureString(l, font).Height);

                // work out the starting position
                var y = (imageSize.Height - totalLineHeight) / 2;
                foreach (var iterator in lines)
                {
                    // get this line
                    var line = iterator;

                    // if the line is empty, make it a space, so it has some size
                    if (line == string.Empty) line = " ";

                    // get the size of this line
                    var lineSize = graphics.MeasureString(line, font);

                    // workout the location for this line
                    var location = new PointF(0, y);

                    // draw the background
                    graphics.FillRectangle(new SolidBrush(backgroundColour), location.X, location.Y, lineSize.Width, lineSize.Height);

                    // draw the caption
                    graphics.DrawString(line, font, brush, location);
                    y += lineSize.Height;
                }

            }
        }

        private void CaptionMiddleRight(Graphics graphics, Size imageSize, string caption, Font font, Brush brush, Color backgroundColour)
        {
            using (var logger = _logger.Block())
            {
                // load the lines from the top
                logger.Trace($"Splitting caption \"{caption}\" into lines that fit on image...");
                var lines = _lineWrapService.WrapToFitFromTop(graphics, imageSize, caption, font);

                // calculate the total line height
                logger.Trace("Calculating total height for caption...");
                var totalLineHeight = lines.Sum(l => graphics.MeasureString(l, font).Height);

                // work out the starting position
                var y = (imageSize.Height - totalLineHeight) / 2;
                foreach (var iterator in lines)
                {
                    // get this line
                    var line = iterator;

                    // if the line is empty, make it a space, so it has some size
                    if (line == string.Empty) line = " ";

                    // get the size of this line
                    var lineSize = graphics.MeasureString(line, font);

                    // workout the location for this line
                    var location = new PointF(imageSize.Width - lineSize.Width, y);

                    // draw the background
                    graphics.FillRectangle(new SolidBrush(backgroundColour), location.X, location.Y, lineSize.Width, lineSize.Height);

                    // draw the caption
                    graphics.DrawString(line, font, brush, location);
                    y += lineSize.Height;
                }

            }
        }

        private void CaptionTopCentre(Graphics graphics, Size imageSize, string caption, Font font, Brush brush, Color backgroundColour)
        {
            using (var logger = _logger.Block())
            {
                // load the lines from the top
                logger.Trace($"Splitting caption \"{caption}\" into lines that fit on image...");
                var lines = _lineWrapService.WrapToFitFromTop(graphics, imageSize, caption, font);

                // work out the starting position
                var y = 0f;
                foreach (var iterator in lines)
                {
                    // get this line
                    var line = iterator;

                    // if the line is empty, make it a space, so it has some size
                    if (line == string.Empty) line = " ";

                    // get the size of this line
                    var lineSize = graphics.MeasureString(line, font);

                    // workout the location for this line
                    var location = new PointF((imageSize.Width - lineSize.Width) / 2, y);

                    // draw the background
                    graphics.FillRectangle(new SolidBrush(backgroundColour), location.X, location.Y, lineSize.Width, lineSize.Height);

                    // draw the caption
                    graphics.DrawString(line, font, brush, location);
                    y += lineSize.Height;
                }

            }
        }

        private void CaptionTopLeft(Graphics graphics, Size imageSize, string caption, Font font, Brush brush, Color backgroundColour)
        {
            using (var logger = _logger.Block())
            {
                // load the lines from the top
                logger.Trace($"Splitting caption \"{caption}\" into lines that fit on image...");
                var lines = _lineWrapService.WrapToFitFromTop(graphics, imageSize, caption, font);

                // work out the starting position
                var y = 0f;
                foreach (var iterator in lines)
                {
                    // get this line
                    var line = iterator;

                    // if the line is empty, make it a space, so it has some size
                    if (line == string.Empty) line = " ";

                    // get the size of this line
                    var lineSize = graphics.MeasureString(line, font);

                    // workout the location for this line
                    var location = new PointF(0, y);

                    // draw the background
                    graphics.FillRectangle(new SolidBrush(backgroundColour), location.X, location.Y, lineSize.Width, lineSize.Height);

                    // draw the caption
                    graphics.DrawString(line, font, brush, location);
                    y += lineSize.Height;
                }


            }
        }

        private void CaptionTopRight(Graphics graphics, Size imageSize, string caption, Font font, Brush brush, Color backgroundColour)
        {
            using (var logger = _logger.Block())
            {
                // load the lines from the top
                logger.Trace($"Splitting caption \"{caption}\" into lines that fit on image...");
                var lines = _lineWrapService.WrapToFitFromTop(graphics, imageSize, caption, font);

                // work out the starting position
                var y = 0f;
                foreach (var iterator in lines)
                {
                    // get this line
                    var line = iterator;

                    // if the line is empty, make it a space, so it has some size
                    if (line == string.Empty) line = " ";

                    // get the size of this line
                    var lineSize = graphics.MeasureString(line, font);

                    // workout the location for this line
                    var location = new PointF(imageSize.Width - lineSize.Width, y);

                    // draw the background
                    graphics.FillRectangle(new SolidBrush(backgroundColour), location.X, location.Y, lineSize.Width, lineSize.Height);

                    // draw the caption
                    graphics.DrawString(line, font, brush, location);
                    y += lineSize.Height;
                }
            }
        }


        private float GetFontSize(Graphics graphics, string fontName, FontStyle fontStyle, string caption, float height)
        {
            var lastSize = 0F;

            using (var logger = _logger.Block())
            {
                for (var i = 1F; ; i += 0.5F)
                {
                    logger.Trace($@"Creating a new font instance for ""{fontName}""...");
                    var font = new Font(fontName, i, fontStyle);

                    // now measure the string
                    var size = graphics.MeasureString(caption, font);

                    // if it is bigger than the desired height, we have found the correct size
                    if (size.Height > height) break;

                    // release the font memory
                    font.Dispose();

                    // save the last font size that fit
                    lastSize = i;
                }

                return lastSize;
            }
        }
    }
}
