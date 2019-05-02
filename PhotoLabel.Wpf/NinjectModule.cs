namespace PhotoLabel.Wpf
{
    public class NinjectModule : Ninject.Modules.NinjectModule
    {
        public override void Load()
        {
            Bind<SingleTaskScheduler>().ToSelf().InSingletonScope();
        }
    }
}