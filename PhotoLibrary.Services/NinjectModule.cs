namespace PhotoLibrary.Services
{
    public class NinjectModule : Ninject.Modules.NinjectModule
    {
        public override void Load()
        {
            Bind<IImageMetadataService>().To<ImageMetadataService>().InSingletonScope();
            Bind<IImageService>().To<ImageService>().InSingletonScope();
            Bind<ILogService>().To<LogService>().InSingletonScope();
            Bind<IRecentlyUsedFilesService>().To<RecentlyUsedFilesService>().InSingletonScope();
            Bind<ILineWrapService>().To<LineWrapService>().InSingletonScope();
        }
    }
}