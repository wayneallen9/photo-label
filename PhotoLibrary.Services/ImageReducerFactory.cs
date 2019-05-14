using Ninject.Parameters;
using Shared;

namespace PhotoLabel.Services
{
    public static class ImageReducerFactory
    {
        public static IImageReducer Create(ImageFormat imageFormat)
        {
            switch (imageFormat)
            {
                case ImageFormat.Jpeg:
                    return Injector.Get<JpegImageReducer>();
                default:
                    var constructorArgument = new ConstructorArgument("imageFormat", imageFormat);

                    return Injector.Get<DefaultImageReducer>(constructorArgument);
            }
        }
    }
}