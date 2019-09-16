using Shared;
using Shared.Attributes;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace PhotoLabel.Services
{
    [Singleton]
    public class BrightnessService : IBrightnessService
    {
        #region variables
        private readonly ILogger _logger;
        #endregion

        public BrightnessService(
            ILogger logger)
        {
            // save dependencies
            _logger = logger;
        }

        public Bitmap Adjust(Bitmap source, int brightness)
        {
            using (var logger = _logger.Block())
            {
                var brightnessAmount = brightness / 100.0f;

                logger.Trace($"Creating image {source.Width}px x {source.Height}px...");
                var image = new Bitmap(source.Width, source.Height, source.PixelFormat);

                logger.Trace("Creating matrix to adjust colour...");
                var adjustArray = new[]
                {
                    new[] { 1.0f, 0, 0, 0, 0},
                    new[] { 0, 1.0f, 0, 0, 0},
                    new[] { 0, 0, 1.0f, 0, 0},
                    new[] { 0, 0, 0, 1.0f, 0},
                    new [] { brightnessAmount, brightnessAmount, brightnessAmount, 0, 1}
                };

                logger.Trace("Creating the image attributes...");
                var imageAttributes = new ImageAttributes();
                imageAttributes.ClearColorMatrix();
                imageAttributes.SetColorMatrix(new ColorMatrix(adjustArray), ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                logger.Trace("Copying source image onto copy...");
                using (var imageGraphics = Graphics.FromImage(image))
                {
                    logger.Trace("Setting up graphics manager...");
                    imageGraphics.SmoothingMode = SmoothingMode.HighQuality;
                    imageGraphics.CompositingQuality = CompositingQuality.HighQuality;
                    imageGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    imageGraphics.DrawImage(source, new Rectangle(0, 0, image.Width, image.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
                }

                return image;
            }
        }
    }
}