using Ninject;
namespace PhotoLabel
{
    public static class NinjectKernel
    {
        #region variables
        private static readonly IKernel kernel;
        #endregion

        static NinjectKernel()
        {
            // create the kernel
            kernel = new StandardKernel();

            // load the injections
            kernel.Load("PhotoLibrary.*.dll");
        }

        public static T Get<T>()
        {
            return kernel.Get<T>();
        }
    }
}