using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoLabel.Services
{
    public class JpegImageReducer : IImageReducer
    {
        public JpegImageReducer(
            IConfigurationService configurationService,
            IImageService imageService,
            ILogService logService)
        {
            // save dependencies
            _configurationService = configurationService;
            _imageService = imageService;
            _logService = logService;
        }

        public Stream Reduce(Bitmap image)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Starting with 100% quality...");
                return Reduce(image, 100, 1, 100);
            }
            finally
            {
                _logService.TraceExit();
            }

        }

        private Stream Reduce(Bitmap image, byte currentQuality, byte minQuality, int maxQuality)
        {
            _logService.TraceEnter();
            try
            {
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
            finally
            {
                _logService.TraceExit();
            }
        }

        #region variables

        private readonly IConfigurationService _configurationService;
        private readonly IImageService _imageService;
        private readonly ILogService _logService;

        #endregion
    }
}
