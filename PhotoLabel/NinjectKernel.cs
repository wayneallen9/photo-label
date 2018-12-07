using Ninject;

namespace PhotoLabel
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

            // load the injections
            Kernel.Load("PhotoLabel.*.dll");
        }

        public static T Get<T>()
        {
            return Kernel.Get<T>();
        }
    }
}