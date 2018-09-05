using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Ninject;
namespace PhotoLibrary
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region variables
        private IKernel _kernel;
        #endregion

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // create the Ninject kernel
            _kernel = new StandardKernel();

            // set-up the bindings
            _kernel.Load("PhotoLibrary.*.dll");

            // set-up the window
            Current.MainWindow = _kernel.Get<MainWindow>();

            // show the window
            Current.MainWindow.Show();
        }
    }
}
