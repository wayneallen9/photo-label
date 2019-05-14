using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using PhotoLabel.Services;
using PhotoLabel.Wpf.Annotations;
using PhotoLabel.Wpf.Properties;
using Shared;

namespace PhotoLabel.Wpf
{
    public class SubFolderViewModel : INotifyPropertyChanged
    {
        public SubFolderViewModel()
        {
            // get dependencies
            _dialogger = Injector.Get<IDialogService>();
            _logger = Injector.Get<ILogger>();
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                using (var logger = _logger.Block())
                {
                    try
                    {
                        logger.Trace($"Checking if value of {nameof(IsSelected)} has changed...");
                        if (_isSelected == value)
                        {
                            logger.Trace($"Value of {nameof(IsSelected)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($"Setting value of {nameof(IsSelected)} to {value}...");
                        _isSelected = value;

                        OnPropertyChanged();
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                    }
                }

            }
        }

        protected void OnError(Exception error)
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace("Checking if running on UI thread...");
                    if (Application.Current?.Dispatcher.CheckAccess() == false)
                    {
                        logger.Trace("Not running on UI thread.  Dispatching to UI thread...");
                        Application.Current?.Dispatcher.Invoke(new OnErrorDelegate(OnError), DispatcherPriority.Input,
                            error);

                        return;
                    }

                    logger.Trace("Logging error...");
                    logger.Error(error);

                    logger.Trace($"Notifying user of error...");
                    _dialogger.Error(Resources.ErrorText);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Checking if running on UI thread...");
                if (Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    logger.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Application.Current.Dispatcher.Invoke(new OnPropertyChangedDelegate(OnPropertyChanged),
                        DispatcherPriority.ApplicationIdle,
                        propertyName);

                    return;
                }

                logger.Trace("Running on UI thread.  Executing...");
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string Path
        {
            get => _path;
            set
            {
                using (var logger = _logger.Block())
                {
                    try
                    {
                        logger.Trace($"Checking if value of {nameof(Path)} has changed...");
                        if (_path == value)
                        {
                            logger.Trace($"Value of {nameof(Path)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($"Setting value of {nameof(Path)} to {value}...");
                        _path = value;

                        OnPropertyChanged();
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                    }
                }
            }
        }

        #region delegates
        private delegate void OnErrorDelegate(Exception error);
        private delegate void OnPropertyChangedDelegate(string propertyName);
        #endregion

        #region variables

        private readonly IDialogService _dialogger;
        private bool _isSelected;
        private readonly ILogger _logger;
        private string _path;

        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

    }
}
