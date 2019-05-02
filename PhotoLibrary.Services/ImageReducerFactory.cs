using Ninject.Parameters;
using PhotoLabel.DependencyInjection;

namespace PhotoLabel.Services
{
    public static class ImageReducerFactory
    {
        public static IImageReducer Create(ImageFormat imageFormat)
        {
            switch (imageFormat)
            {
                case ImageFormat.Jpeg:
                    return NinjectKernel.Get<JpegImageReducer>();
                default:
                    var constructorArgument = new ConstructorArgument("imageFormat", imageFormat);

                    return NinjectKernel.Get<DefaultImageReducer>(constructorArgument);
            }
        }
    }
}