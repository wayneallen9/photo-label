using System.Windows;

namespace PhotoLabel.Services
{
    public class NavigationService : INavigationService
    {
        public NavigationService(
            ILogService logService)
        {
            // save dependencies
            _logService = logService;
        }

        public bool? ShowDialog<T>(object dataContext) where T : Window, new()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Creating new window...");
                var window = new T();

                _logService.Trace("Assigning data context...");
                window.DataContext = dataContext;

                _logService.Trace("Showing window as dialog...");
                return window.ShowDialog();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        #region variables

        private readonly ILogService _logService;

        #endregion
    }
}