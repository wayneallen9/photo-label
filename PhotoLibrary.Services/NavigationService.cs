using System;
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
                _logService.Trace("Saving current parent window...");
                var parentWindow = _parentWindow ?? Application.Current?.MainWindow;

                _logService.Trace("Creating new window...");
                var window = new T();

                _logService.Trace("Assigning data context...");
                window.DataContext = dataContext;

                _logService.Trace("Assigning parent window...");
                if (parentWindow?.IsVisible == true) window.Owner = parentWindow;

                _logService.Trace("Saving new window as parent...");
                _parentWindow = window;

                _logService.Trace("Showing window as dialog...");
                var result = window.ShowDialog();

                _logService.Trace("Resetting parent window...");
                _parentWindow = parentWindow;

                return result;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        #region variables

        private readonly ILogService _logService;
        private Window _parentWindow;

        #endregion
    }
}