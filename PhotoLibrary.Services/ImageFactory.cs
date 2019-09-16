using Ninject.Parameters;
using Shared;
using Shared.Attributes;
using System;

namespace PhotoLabel.Services
{
    [Singleton]
    public class ImageFactory : IImageFactory
    {
        #region variables
        private readonly ILogger _logger;
        #endregion

        public ImageFactory(
            ILogger logger)
        {
            // save dependencies
            _logger = logger;
        }

        public IImageService Create(bool useCanvas, int? canvasWidth, int? canvasHeight)
        {
            using (var logger = _logger.Block())
            {
                // which image service should be created?
                if (useCanvas)
                {
                    if (!canvasWidth.HasValue) throw new ArgumentNullException(nameof(canvasWidth));
                    if (!canvasHeight.HasValue) throw new ArgumentNullException(nameof(canvasHeight));

                    var widthParameter = new ConstructorArgument("canvasWidth", canvasWidth.Value);
                    var heightParameter = new ConstructorArgument("canvasHeight", canvasHeight.Value);

                    return Injector.Get<ImageWithCanvasService>(widthParameter, heightParameter);
                }

                return Injector.Get<ImageService>();
            }
        }
    }
}
