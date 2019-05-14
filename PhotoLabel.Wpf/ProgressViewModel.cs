using PhotoLabel.Services;
using PhotoLabel.Wpf.Properties;
using Shared;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace PhotoLabel.Wpf
{
    public class ProgressViewModel : INotifyPropertyChanged
    {
        public ProgressViewModel(
            IDialogService dialogService,
            ILogger logger)
        {
            // save dependencies
            _dialogService = dialogService;
            _logger = logger;

            // initialise variables
            _maximum = 100;
            _value = 0;
        }

        public bool Close
        {
            get => _close;
            set
            {
                _close = value;

                OnPropertyChanged();
            }
        }

        public string Directory
        {
            get => _directory;
            set
            {
                using (var logger = _logger.Block())
                {
                    try
                    {
                        logger.Trace($"Checking if value of {nameof(Directory)} has changed...");
                        if (_directory == value)
                        {
                            logger.Trace($"Value of {nameof(Directory)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($@"Setting value of {nameof(Directory)} to ""{value}""...");
                        _directory = value;

                        OnPropertyChanged();
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                    }
                }
            }
        }

        public int Maximum
        {
            get => _maximum;
            set
            {
                using (var logger = _logger.Block())
                {
                    try
                    {
                        logger.Trace($"Checking if value of {nameof(Maximum)} has changed...");
                        if (_maximum == value)
                        {
                            logger.Trace($"Value of {nameof(Maximum)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($"Setting value of {nameof(Maximum)} to {value}...");
                        _maximum = value;

                        OnPropertyChanged();
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                    }
                }
            }
        }

        private void OnError(Exception error)
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
                    }

                    logger.Trace("Logging error...");
                    logger.Error(error);

                    logger.Trace($"Notifying user of error...");
                    _dialogService.Error(Resources.ErrorText);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
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

        public int Value
        {
            get => _value;
            set
            {
                using (var logger = _logger.Block())
                {
                    try
                    {
                        logger.Trace($"Checking if value of {nameof(Value)} has changed...");
                        if (_value == value)
                        {
                            logger.Trace($"Value of {nameof(Value)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($"Setting value of {nameof(Value)} to {value}...");
                        _value = value;

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

        private bool _close;
        private readonly IDialogService _dialogService;
        private readonly ILogger _logger;
        private int _maximum;
        private string _directory;
        private int _value;
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}
