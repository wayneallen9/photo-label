using System;
using System.Drawing;
using System.IO;

namespace PhotoLabel.Services
{
    public class DefaultImageReducer : IImageReducer
    {
        public DefaultImageReducer(
            ImageFormat imageFormat,
            IConfigurationService configurationService,
            IImageService imageService,
            ILogService logService)
        {
            // save dependencies
            _imageFormat = imageFormat;
            _configurationService = configurationService;
            _imageService = imageService;
            _logService = logService;
        }

        public Stream Reduce(Bitmap image)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Starting with current image size...");
                return Reduce(image, 100, 1, 100);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private Stream Reduce(Bitmap image, long currentRatio, long minRatio, long maxRatio)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Reducing image quality...");
                using (var reducedQualityStream = _imageService.ReduceQuality(image, currentRatio)) { 
                    _logService.Trace("Converting it to target format...");
                    var reducedQualityImage = (Bitmap)Image.FromStream(reducedQualityStream);
                    var memoryStream = new MemoryStream();
                    reducedQualityImage.Save(memoryStream, _imageFormat == ImageFormat.Bmp ? System.Drawing.Imaging.ImageFormat.Bmp : System.Drawing.Imaging.ImageFormat.Png);

                    // is it a perfect match?
                    if ((ulong)memoryStream.Length == _configurationService.MaxImageSize)
                    {
                        return memoryStream;
                    }

                    // is it still too big?
                    if ((ulong) memoryStream.Length > _configurationService.MaxImageSize)
                    {
                        // release the memory
                        memoryStream.Dispose();

                        // this is the biggest ratio that we have tried
                        maxRatio = currentRatio;
                        currentRatio = (currentRatio - minRatio) / 2 + minRatio;

                        return Reduce(image, currentRatio, minRatio, maxRatio);
                    }

                    // if this is the biggest ratio we have tried, this is the best fit
                    if (Math.Abs(maxRatio - currentRatio) < 2)
                    {
                        // reset to the first position
                        memoryStream.Position = 0;

                        return memoryStream;
                    }

                    // release the memory
                    memoryStream.Dispose();

                    // we can try something a bit larger
                    minRatio = currentRatio;
                    currentRatio = (maxRatio - currentRatio) / 2 + currentRatio;

                    return Reduce(image, currentRatio, minRatio, maxRatio);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        #region variables

        private readonly IConfigurationService _configurationService;
        private readonly ImageFormat _imageFormat;
        private readonly IImageService _imageService;
        private readonly ILogService _logService;

        #endregion
    }
}