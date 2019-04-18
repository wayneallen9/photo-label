using NLog;
using System.Threading.Tasks;

namespace PhotoLabel.Services
{
    // ReSharper disable once UnusedMember.Global
    public class NinjectModule : Ninject.Modules.NinjectModule
    {
        public override void Load()
        {
            Bind<IConfigurationService>().To<ConfigurationService>().InSingletonScope();
            Bind<IDirectoryOpenerService>().To<DirectoryOpenerService>().InSingletonScope();
            Bind<IImageLoaderService>().To<ImageLoaderService>().InSingletonScope();
            Bind<IImageMetadataService>().To<ImageMetadataService>().InSingletonScope();
            Bind<IImageService>().To<ImageService>().InSingletonScope();
            Bind<IIndentationService>().To<IndentationService>().InThreadScope();
            Bind<ILogService>().To<LogService>().InSingletonScope();
            Bind<IPercentageService>().To<PercentageService>().InSingletonScope();
            Bind<IRecentlyUsedDirectoriesService>().To<RecentlyUsedDirectoriesService>().InSingletonScope();
            Bind<ILineWrapService>().To<LineWrapService>().InSingletonScope();
            Bind<IQuickCaptionService>().To<QuickCaptionService>().InSingletonScope();
            Bind<IXmlFileSerialiser>().To<XmlFileSerialiser>().InSingletonScope();

            Bind<ILogger>()
                .ToMethod(context => LogManager.GetCurrentClassLogger(context.Request.Target?.Member.DeclaringType))
                .InTransientScope();
        }
    }
}