using PhotoLabel.Services;
using Shared;
using System;
using System.Windows;
using System.Windows.Threading;

namespace PhotoLabel.Wpf
{
    /// <inheritdoc />
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // create the dependency injections
            Injector.Bind(GetType(), typeof(ConfigurationService));
            Injector.Bind<SingleTaskScheduler>();

            // create the main window
            var mainWindow = Injector.Get<MainWindow>();

            // assign the view model to the window
            var mainWindowViewModel = Injector.Get<MainWindowViewModel>();
            mainWindow.DataContext = mainWindowViewModel;
            
            // show the window
            mainWindow.Show();
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // get dependencies
            var logService = Injector.Get<ILogger>();

            using (var logger = logService.Block())
            {
                try
                {
                    logger.Error(e.Exception);

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
                }
            }
        }
    }
}