using NLog;

namespace PhotoLabel.Services
{
    // ReSharper disable once UnusedMember.Global
    public class NinjectModule : Ninject.Modules.NinjectModule
    {
        public override void Load()
        {
            Bind<IBrowseService>().To<BrowseService>().InSingletonScope();
            Bind<IConfigurationService>().To<ConfigurationService>().InSingletonScope();
            Bind<IImageMetadataService>().To<ImageMetadataService>().InSingletonScope();
            Bind<IImageService>().To<ImageService>().InSingletonScope();
            Bind<IIndentationService>().To<IndentationService>().InThreadScope();
            Bind<ILogService>().To<LogService>().InTransientScope();
            Bind<IRecentlyUsedDirectoriesService>().To<RecentlyUsedDirectoriesService>().InSingletonScope();
            Bind<ILineWrapService>().To<LineWrapService>().InSingletonScope();
            Bind<INavigationService>().To<NavigationService>().InSingletonScope();
            Bind<IWhereService>().To<WhereService>().InSingletonScope();
            Bind<IXmlFileSerialiser>().To<XmlFileSerialiser>().InSingletonScope();

            Bind<ILogger>()
                .ToMethod(context => LogManager.GetCurrentClassLogger())
                .InSingletonScope();
        }
    }
}