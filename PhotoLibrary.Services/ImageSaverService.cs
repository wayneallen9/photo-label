using Shared;
using Shared.Attributes;
using System.Drawing;
using System.IO;

namespace PhotoLabel.Services
{
    [Singleton]
    public class ImageSaverService : IImageSaverService
    {
        #region variables
        private readonly IConfigurationService _configurationService;
        private readonly ILogger _logger;
        #endregion

        public ImageSaverService(
            IConfigurationService configurationService,
            ILogger logger)
        {
            // save dependencies
            _configurationService = configurationService;
            _logger = logger;
        }

        public void Save(Bitmap bitmap, string filename, ImageFormat imageFormat)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Getting format to save...");
                System.Drawing.Imaging.ImageFormat imagingImageFormat;
                switch (imageFormat)
                {
                    case ImageFormat.Bmp:
                        imagingImageFormat = System.Drawing.Imaging.ImageFormat.Bmp;

                        break;
                    case ImageFormat.Jpeg:
                        imagingImageFormat = System.Drawing.Imaging.ImageFormat.Jpeg;

                        break;
                    default:
                        imagingImageFormat = System.Drawing.Imaging.ImageFormat.Png;

                        break;
                }

                logger.Trace($@"Creating ""{filename}""...");
                using (var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    logger.Trace("Checking if there is a size limitation...");
                    if (_configurationService.MaxImageSize != null)
                    {
                        logger.Trace("Reducing image to fit size limitation...");
                        var imageReducer = ImageReducerFactory.Create(imageFormat);
                        using (var imageStream = imageReducer.Reduce(bitmap))
                        {
                            logger.Trace("Saving reduced image to disk...");
                            imageStream.CopyTo(fileStream);
                        }
                    }
                    else
                    {
                        logger.Trace($@"Saving image to ""{filename}""...");
                        bitmap.Save(fileStream, imagingImageFormat);
                    }
                }
            }
        }
    }
}