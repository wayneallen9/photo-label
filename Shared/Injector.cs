using Ninject;
using Ninject.Extensions.Conventions;
using Ninject.Parameters;
using Shared.Attributes;
using System;
using System.Reflection;
using Ninject.Infrastructure.Language;

namespace Shared
{
    public static class Injector
    {
        #region variables

        private static readonly IKernel Kernel;
        #endregion

        static Injector()
        {
            // create the Ninject kernel
            Kernel = new StandardKernel();

            // include all of the classes in this assembly
            Bind(Assembly.GetExecutingAssembly());

            // create the binding for the NLog logger
            Kernel.Bind<NLog.ILogger>().ToMethod(context => NLog.LogManager.GetCurrentClassLogger()).InSingletonScope();
        }

        public static void Bind(params Type[] types)
        {
            foreach (var type in types)
            {
                Bind(type.Assembly);
            }
        }

        public static void Bind<T>()
        {
            if (typeof(T).HasAttribute<SingletonAttribute>())
            {
                Kernel.Bind<T>().ToSelf().InSingletonScope();
            }
            else if (typeof(T).HasAttribute<ThreadAttribute>())
            {
                Kernel.Bind<T>().ToSelf().InThreadScope();
            }
            else
                Kernel.Bind<T>().ToSelf().InTransientScope();
        }

        private static void Bind(Assembly assembly)
        {
            // bind the singleton classes
            Kernel.Bind(x =>
                x.From(assembly).SelectAllClasses().WithAttribute<SingletonAttribute>().BindDefaultInterface()
                    .Configure(configuration => configuration.InSingletonScope()));

            // bind the thread classes
            Kernel.Bind(x =>
                x.From(assembly).SelectAllClasses().WithAttribute<ThreadAttribute>().BindDefaultInterface()
                    .Configure(configuration => configuration.InThreadScope()));

            // bind the transient classes
            Kernel.Bind(x =>
                x.From(assembly).SelectAllClasses().WithoutAttribute<SingletonAttribute>()
                    .WithoutAttribute<ThreadAttribute>().BindDefaultInterface()
                    .Configure(configuration => configuration.InTransientScope()));
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