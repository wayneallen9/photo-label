﻿namespace PhotoLabel.Services
{
    public class NinjectModule : Ninject.Modules.NinjectModule
    {
        public override void Load()
        {
            Bind<IImageLoaderService>().To<ImageLoaderService>().InSingletonScope();
            Bind<IImageMetadataService>().To<ImageMetadataService>().InSingletonScope();
            Bind<IImageService>().To<ImageService>().InSingletonScope();
            Bind<ILocaleService>().To<LocaleService>().InSingletonScope();
            Bind<ILogService>().To<LogService>().InSingletonScope();
            Bind<IRecentlyUsedFilesService>().To<RecentlyUsedFilesService>().InSingletonScope();
            Bind<ILineWrapService>().To<LineWrapService>().InSingletonScope();
            Bind<ITimerService>().To<TimerService>();
        }
    }
}