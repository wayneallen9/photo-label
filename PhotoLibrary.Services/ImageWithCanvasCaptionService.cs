using Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoLabel.Services
{
    public class ImageWithCanvasCaptionService : IImageCaptionService
    {
        #region variables
        private readonly int _canvasHeight;
        private readonly int _canvasWidth;
        private readonly IImageCaptionService _imageCaptionService;
        private readonly ILogger _logger;
        #endregion

        public ImageWithCanvasCaptionService(
            int? canvasHeight,
            int? canvasWidth,
            IImageCaptionService imageCaptionService,
            ILogger logger)
        {
            _canvasHeight = canvasHeight ?? throw new ArgumentNullException(nameof(canvasHeight));
            _canvasWidth = canvasWidth ?? throw new ArgumentNullException(nameof(canvasWidth));
            _imageCaptionService = imageCaptionService;
            _logger = logger;
        }

        public Bitmap Caption(Bitmap original, string caption, bool? appendDateTakenToCaption, string dateTaken, Rotations? rotation, CaptionAlignments? captionAlignment, string fontName, float fontSize, string fontType, bool fontBold, Brush brush, Color backgroundColour, CancellationToken cancellationToken)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($"Creating a canvas {_canvasWidth}px x {_canvasHeight}px...");
                var canvas = new Bitmap(_canvasWidth, _canvasHeight);
                try
                {
                    if (cancellationToken.IsCancellationRequested) return null;
                    logger.Trace($@"Resizing original image to fit a canvas {_canvasWidth}px x {_canvasHeight}px...");
                    var aspectRatio = Math.Max(original.Height / (double)_canvasHeight, original.Width / (double)_canvasWidth);
                    var resizedHeight = Convert.ToInt32(original.Height / aspectRatio);
                    var resizedWidth = Convert.ToInt32(original.Width / aspectRatio);
                    var resizedX = (_canvasWidth - resizedWidth) / 2;
                    var resizedY = (_canvasHeight - resizedHeight) / 2;
                    using (var resized = new Bitmap(resizedWidth, resizedHeight))
                    {
                        if (cancellationToken.IsCancellationRequested) return null;
                        using (var graphics = Graphics.FromImage(resized))
                        {
                            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                            graphics.DrawImage(original, 0, 0, resizedWidth, resizedHeight);
                        }

                        using (var captioned = _imageCaptionService.Caption(resized, caption, appendDateTakenToCaption, dateTaken, rotation, captionAlignment, fontName, fontSize, fontType, fontBold, brush, backgroundColour, cancellationToken))
                        {
                            if (cancellationToken.IsCancellationRequested) return null;
                            using (var graphics = Graphics.FromImage(canvas))
                            {
                                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                                graphics.DrawImage(captioned, resizedX, resizedY, resizedWidth, resizedHeight);
                            }
                        }
                    }

                    if (cancellationToken.IsCancellationRequested) return null;
                    return canvas;
                }
                finally
                {
                    if (cancellationToken.IsCancellationRequested) canvas.Dispose();
                }
            }
        }
    }
}
