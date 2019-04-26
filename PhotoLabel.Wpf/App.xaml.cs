using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using PhotoLabel.Services;

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
            var mainWindowViewModel = NinjectKernel.Get<MainWindowViewModel>();
            mainWindowViewModel.Subscribe(mainWindow);
            mainWindow.DataContext = mainWindowViewModel;
            
            // show the window
            mainWindow.Show();
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // get dependencies
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Error(e.Exception);

                MessageBox.Show(
                    "An unexpected error was encountered completing an operation.  The error details can be found in the application log.",
                    "Unexpected Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                // flag that the exception has been handled
                e.Handled = true;

                logService.TraceExit();
            }
        }
    }
}
