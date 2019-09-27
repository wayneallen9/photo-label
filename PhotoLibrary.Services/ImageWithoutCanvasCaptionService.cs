using Shared;
using System.Drawing;
using System.Threading;

namespace PhotoLabel.Services
{
    public class ImageWithoutCanvasCaptionService : IImageCaptionService
    {
        #region variables
        private readonly IImageCaptionService _imageCaptionService;
        private readonly IImageRotationService _imageRotationService;
        private readonly ILogger _logger;
        #endregion

        public ImageWithoutCanvasCaptionService(
            IImageCaptionService imageCaptionService,
            IImageRotationService imageRotationService,
            ILogger logger)
        {
            _imageCaptionService = imageCaptionService;
            _imageRotationService = imageRotationService;
            _logger = logger;
        }

        public Bitmap Caption(Bitmap original, string caption, bool? appendDateTakenToCaption, string dateTaken, Rotations rotation, CaptionAlignments? captionAlignment, string fontName, float fontSize, string fontType, bool fontBold, Brush brush, Color backgroundColour, CancellationToken cancellationToken)
        {
            using (var logger = _logger.Block())
            {
                if (cancellationToken.IsCancellationRequested) return null;
                logger.Trace("Rotating image...");
                var rotated = _imageRotationService.Rotate(original, rotation);

                if (cancellationToken.IsCancellationRequested) return null;
                logger.Trace($@"Captioning image with ""{caption}""...");
                return _imageCaptionService.Caption(rotated, caption, appendDateTakenToCaption, dateTaken, rotation, captionAlignment, fontName, fontSize, fontType, fontBold, brush, backgroundColour, cancellationToken);
            }
        }
    }
}