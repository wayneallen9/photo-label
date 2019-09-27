using Ninject.Parameters;
using Shared;
using Shared.Attributes;
using System;

namespace PhotoLabel.Services
{
    [Singleton]
    public class ImageCaptionServiceFactory : IImageCaptionServiceFactory
    {
        #region variables
        private readonly ILogger _logger;
        #endregion

        public ImageCaptionServiceFactory(
            ILogger logger)
        {
            // save dependencies
            _logger = logger;
        }

        public IImageCaptionService Create(bool useCanvas, int? canvasWidth, int? canvasHeight)
        {
            using (var logger = _logger.Block())
            {
                if (useCanvas)
                {
                    if (canvasHeight == null) throw new ArgumentNullException(nameof(canvasHeight));
                    if (canvasWidth == null) throw new ArgumentNullException(nameof(canvasWidth));

                    var canvasHeightParameter = new ConstructorArgument("canvasHeight", canvasHeight);
                    var canvasWidthParameter = new ConstructorArgument("canvasWidth", canvasWidth);
                    return Injector.Get<ImageWithCanvasCaptionService>(canvasHeightParameter, canvasWidthParameter);
                }
                else
                    return Injector.Get<ImageWithoutCanvasCaptionService>();
            }
        }
    }
}