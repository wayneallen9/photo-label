using NLog;
using Shared;

namespace PhotoLabel.Services
{
    // ReSharper disable once UnusedMember.Global
    public class NinjectModule : Ninject.Modules.NinjectModule
    {
        public override void Load()
        {
            Bind<IDialogger>().To<Dialogger>().InSingletonScope();
            Bind<IConfigurationService>().To<ConfigurationService>().InSingletonScope();
            Bind<IFolderService>().To<FolderService>().InSingletonScope();
            Bind<IImageMetadataService>().To<ImageMetadataService>().InSingletonScope();
            Bind<IImageService>().To<ImageService>().InSingletonScope();
            Bind<IIndentationService>().To<IndentationService>().InThreadScope();
            Bind<ILogger>().To<logger>().InTransientScope();
            Bind<IOpacityService>().To<OpacityService>().InSingletonScope();
            Bind<IRecentlyUsedFoldersService>().To<RecentlyUsedDirectoriesService>().InSingletonScope();
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