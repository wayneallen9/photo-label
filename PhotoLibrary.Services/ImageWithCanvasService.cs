using PhotoLabel.Services.Models;
using Shared;
using Shared.Attributes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;

namespace PhotoLabel.Services
{
    [Singleton]
    public class ImageWithCanvasService : IImageService
    {
        #region variables
        private readonly IBrightnessService _brightnessService;
        private readonly int _canvasHeight;
        private readonly int _canvasWidth;
        private readonly IImageCaptionService _imageCaptionService;
        private readonly ILogger _logger;
        #endregion

        public ImageWithCanvasService(
            int canvasWidth,
            int canvasHeight,
            IBrightnessService brightnessService,
            IImageCaptionService imageCaptionService,
            ILogger logger)
        {
            // save dependencies
            _brightnessService = brightnessService;
            _canvasHeight = canvasHeight;
            _canvasWidth = canvasWidth;
            _imageCaptionService = imageCaptionService;
            _logger = logger;
        }

        public Bitmap Brightness(Bitmap bitmap, int brightness)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($"Adjusting image brightness to {brightness}...");
                return _brightnessService.Adjust(bitmap, brightness);
            }
        }

        public Bitmap Caption(Bitmap image, string caption, bool? appendDateTakenToCaption, string dateTaken, Rotations? rotation, CaptionAlignments? captionAlignment, string fontName, float fontSize, string fontType, bool fontBold, Brush brush, Color backgroundColor, CancellationToken cancellationToken)
        {
            using (var logger = _logger.Block())
            {
                if (cancellationToken.IsCancellationRequested) return null;
                logger.Trace($"Creating a canvas {_canvasWidth}px x {_canvasHeight}px...");
                var canvas = new Bitmap(_canvasWidth, _canvasHeight);
                try
                {
                    if (cancellationToken.IsCancellationRequested) return null;
                    logger.Trace($"Resizing image to fit canvas {_canvasWidth}px x {_canvasHeight}px...");
                    var aspectRatio = Math.Max((double)image.Width / _canvasWidth, (double)image.Height / _canvasHeight);
                    var imageHeight = (int)(image.Height / aspectRatio);
                    var imageWidth = (int)(image.Width / aspectRatio);
                    var imageX = (_canvasWidth - imageWidth) / 2;
                    var imageY = (_canvasHeight - imageHeight) / 2;
                    using (var resizedImage = new Bitmap(imageWidth, imageHeight))
                    {
                        if (cancellationToken.IsCancellationRequested) return null;
                        logger.Trace("Getting graphics manager for new image...");
                        using (var graphics = Graphics.FromImage(resizedImage))
                        {
                            logger.Trace("Setting up graphics manager...");
                            graphics.SmoothingMode = SmoothingMode.HighQuality;
                            graphics.CompositingQuality = CompositingQuality.HighQuality;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                            logger.Trace("Drawing resized image...");
                            graphics.DrawImage(image, 0, 0, imageWidth, imageHeight);
                        }

                        logger.Trace($@"Adding caption ""{caption}"" to image...");
                        using (var resizedWithCaptionImage = _imageCaptionService.Caption(resizedImage, caption, appendDateTakenToCaption, dateTaken, rotation, captionAlignment, fontName, fontSize, fontType, fontBold, brush, backgroundColor, cancellationToken))
                        {
                            if (cancellationToken.IsCancellationRequested) return null;
                            logger.Trace("Getting graphics manager for canvas...");
                            using (var graphics = Graphics.FromImage(canvas))
                            {
                                logger.Trace("Setting up graphics manager...");
                                graphics.SmoothingMode = SmoothingMode.HighQuality;
                                graphics.CompositingQuality = CompositingQuality.HighQuality;
                                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                                logger.Trace("Drawing resized image...");
                                graphics.DrawImage(resizedWithCaptionImage, imageX, imageY, imageWidth, imageHeight);
                            }
                        }
                    }

                    return canvas;
                }
                finally
                {
                    // release memory
                    if (cancellationToken.IsCancellationRequested) canvas.Dispose();
                }
            }
        }

        public List<string> Find(string folderPath)
        {
            throw new NotImplementedException();
        }

        public Bitmap Get(string filename, int width, int height)
        {
            throw new NotImplementedException();
        }

        public ExifData GetExifData(string filename)
        {
            throw new NotImplementedException();
        }

        public string GetFilename(string outputPath, string imagePath, ImageFormat imageFormat)
        {
            throw new NotImplementedException();
        }

        public Bitmap Overlay(Bitmap image, Image overlay, int x, int y)
        {
            throw new NotImplementedException();
        }

        public Stream ReduceQuality(Bitmap image, long quality)
        {
            throw new NotImplementedException();
        }

        public Bitmap Resize(Bitmap image, int width, int height)
        {
            throw new NotImplementedException();
        }

        public void Save(Bitmap image, string filename, ImageFormat format)
        {
            throw new NotImplementedException();
        }
    }
}
