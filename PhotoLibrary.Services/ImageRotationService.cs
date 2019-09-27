using Shared;
using Shared.Attributes;
using System.Drawing;

namespace PhotoLabel.Services
{
    [Singleton]
    public class ImageRotationService : IImageRotationService
    {
        #region variables
        private readonly ILogger _logger;
        #endregion

        public ImageRotationService(
            ILogger logger)
        {
            // save dependencies
            _logger = logger;
        }

        public Bitmap Rotate(Bitmap bitmap, Rotations rotation)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($"Current dimensions are {bitmap.Width}px x {bitmap.Height}px.  Rotating to {rotation}...");
                switch (rotation)
                {
                    case Rotations.Ninety:
                        bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);

                        break;
                    case Rotations.OneEighty:
                        bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);

                        break;
                    case Rotations.TwoSeventy:
                        bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);

                        break;
                }

                logger.Trace($"Returning duplicate image {bitmap.Width}px x {bitmap.Height}px...");
                return new Bitmap(bitmap);
            }
        }
    }
}