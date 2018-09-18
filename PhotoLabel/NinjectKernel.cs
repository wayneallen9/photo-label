using Ninject;
using Ninject.Parameters;
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
            kernel.Load("PhotoLabel.*.dll");
        }

        public static T Get<T>()
        {
            return kernel.Get<T>();
        }

        public static T Get<T>(params IParameter[] parameters)
        {
            return kernel.Get<T>(parameters);
        }
    }
}