using Shared;
using Shared.Attributes;
using System.Windows;

namespace PhotoLabel.Services
{
    [Singleton]
    public class NavigationService : INavigationService
    {
        #region delegates

        private delegate bool? ShowDialogDelegate<T>(object dataContext);
        #endregion

        public NavigationService(
            ILogger logger)
        {
            // save dependencies
            _logger = logger;
        }

        public bool? ShowDialog<T>(object dataContext) where T : Window, new()
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Checking if running on UI thread...");
                if (Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    logger.Trace("Not running on UI thread.  Delegating to UI thread...");
                    return (bool?)Application.Current.Dispatcher.Invoke(new ShowDialogDelegate<T>(ShowDialog<T>), dataContext);
                }

                logger.Trace("Saving current parent window...");
                var parentWindow = _parentWindow ?? Application.Current?.MainWindow;

                logger.Trace("Creating new window...");
                var window = new T();

                logger.Trace("Assigning data context...");
                window.DataContext = dataContext;

                logger.Trace("Assigning parent window...");
                if (parentWindow?.IsVisible == true) window.Owner = parentWindow;

                logger.Trace("Saving new window as parent...");
                _parentWindow = window;

                logger.Trace("Showing window as dialog...");
                var result = window.ShowDialog();

                logger.Trace("Resetting parent window...");
                _parentWindow = parentWindow;

                return result;
            
            }
        }

        #region variables

        private readonly ILogger _logger;
        private Window _parentWindow;

        #endregion
    }
}