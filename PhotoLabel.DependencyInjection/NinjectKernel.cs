using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Ninject;
using Ninject.Parameters;

namespace PhotoLabel.DependencyInjection
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

            // get the start assembly
            var mainAssembly = Assembly.GetEntryAssembly();
            Kernel.Load(mainAssembly);

            // load the injections
            Kernel.Load("PhotoLabel.Services.dll");
        }

        public static T Get<T>()
        {
            return Kernel.Get<T>();
        }

        public static T Get<T>(params IParameter[] parameters)
        {
            return Kernel.Get<T>(parameters);
        }
    }
}