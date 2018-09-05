namespace PhotoLibrary.Services
{
    public class NinjectModule : Ninject.Modules.NinjectModule
    {
        public override void Load()
        {
            Bind<IImageService>().To<ImageService>().InSingletonScope();
            Bind<ILogService>().To<LogService>().InSingletonScope();
        }
    }
}