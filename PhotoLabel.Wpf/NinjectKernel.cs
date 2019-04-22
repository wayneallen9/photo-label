using System.Threading.Tasks;
using Ninject;

namespace PhotoLabel.Wpf
{
    public static class NinjectKernel
    {
        #region variables
        private static readonly IKernel Kernel;
        #endregion

        static NinjectKernel()
        {
            // create the kernel
            Kernel = new StandardKernel();

            // create the bindings for this assembly
            Kernel.Bind<SingleTaskScheduler>().ToSelf().InSingletonScope();

            // load the injections
            Kernel.Load("PhotoLabel.*.dll");
        }

        public static T Get<T>()
        {
            return Kernel.Get<T>();
        }
    }
}