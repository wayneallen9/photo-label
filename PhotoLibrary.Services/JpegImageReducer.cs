using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;

namespace PhotoLabel.Services
{
    public class JpegImageReducer : IImageReducer
    {
        public JpegImageReducer(
            IConfigurationService configurationService,
            IImageService imageService,
            ILogger logger)
        {
            // save dependencies
            _configurationService = configurationService;
            _imageService = imageService;
            _logger = logger;
        }

        public Stream Reduce(Bitmap image)
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Starting with 100% quality...");
                return Reduce(image, 100, 1, 100);
            
            }

        }

        private Stream Reduce(Bitmap image, byte currentQuality, byte minQuality, int maxQuality)
        {
            using (var logger = _logger.Block()) {
                // reduce the size of the image
                var memoryStream = _imageService.ReduceQuality(image, currentQuality);

                // is it a perfect match?
                if ((ulong)memoryStream.Length == _configurationService.MaxImageSize)
                {
                    return memoryStream;
                }

                // is it still too big?
                if ((ulong)memoryStream.Length > _configurationService.MaxImageSize)
                {
                    // release the memory
                    memoryStream.Dispose();

                    // this is the biggest ratio that we have tried
                    maxQuality = currentQuality;
                    currentQuality = (byte)((currentQuality - minQuality) / 2 + minQuality);

                    return Reduce(image, currentQuality, minQuality, maxQuality);
                }

                // if this is the biggest ratio we have tried, this is the best fit
                if (Math.Abs(maxQuality - currentQuality) < 2) return memoryStream;

                // release the memory
                memoryStream.Dispose();

                // we can try something a bit larger
                minQuality = currentQuality;
                currentQuality = (byte)((maxQuality - currentQuality) / 2 + currentQuality);

                return Reduce(image, currentQuality, minQuality, maxQuality);
            
            }
        }

        #region variables

        private readonly IConfigurationService _configurationService;
        private readonly IImageService _imageService;
        private readonly ILogger _logger;

        #endregion
    }
}
