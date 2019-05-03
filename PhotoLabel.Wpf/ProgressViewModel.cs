using PhotoLabel.DependencyInjection;
using PhotoLabel.Services;
using PhotoLabel.Wpf.Properties;
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
            ILogService logService)
        {
            // save dependencies
            _dialogService = dialogService;
            _logService = logService;

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
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(Directory)} has changed...");
                    if (_directory == value)
                    {
                        _logService.Trace($"Value of {nameof(Directory)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($@"Setting value of {nameof(Directory)} to ""{value}""...");
                    _directory = value;

                    OnPropertyChanged();
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public int Maximum
        {
            get => _maximum;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(Maximum)} has changed...");
                    if (_maximum == value)
                    {
                        _logService.Trace($"Value of {nameof(Maximum)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(Maximum)} to {value}...");
                    _maximum = value;

                    OnPropertyChanged();
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        private void OnError(Exception error)
        {
            // get dependencies
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace("Checking if running on UI thread...");
                if (Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    logService.Trace("Not running on UI thread.  Dispatching to UI thread...");
                    Application.Current?.Dispatcher.Invoke(new OnErrorDelegate(OnError), DispatcherPriority.Input,
                        error);
                }

                logService.Trace("Logging error...");
                logService.Error(error);

                logService.Trace($"Notifying user of error...");
                _dialogService.Error(Resources.ErrorText);
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            // get dependencies
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace("Checking if running on UI thread...");
                if (Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Application.Current.Dispatcher.Invoke(new OnPropertyChangedDelegate(OnPropertyChanged),
                        DispatcherPriority.ApplicationIdle,
                        propertyName);

                    return;
                }

                logService.Trace("Running on UI thread.  Executing...");
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            finally
            {
                logService.TraceExit();
            }

        }

        public int Value
        {
            get => _value;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(Value)} has changed...");
                    if (_value == value)
                    {
                        _logService.Trace($"Value of {nameof(Value)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(Value)} to {value}...");
                    _value = value;

                    OnPropertyChanged();
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
                finally
                {
                    _logService.TraceExit();
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
        private readonly ILogService _logService;
        private int _maximum;
        private string _directory;
        private int _value;
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}
