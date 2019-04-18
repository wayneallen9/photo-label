using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PhotoLabel.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // create the main window
            var mainWindow = NinjectKernel.Get<MainWindow>();

            // assign the view model to the window
            mainWindow.DataContext = NinjectKernel.Get<MainWindowViewModel>();

            // show the window
            mainWindow.Show();
        }
    }
}
