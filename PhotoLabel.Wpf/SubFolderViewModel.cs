using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using PhotoLabel.DependencyInjection;
using PhotoLabel.Services;
using PhotoLabel.Wpf.Annotations;
using PhotoLabel.Wpf.Properties;

namespace PhotoLabel.Wpf
{
    public class SubFolderViewModel : INotifyPropertyChanged
    {
        public SubFolderViewModel()
        {
            // get dependencies
            _dialogService = NinjectKernel.Get<IDialogService>();
            _logService = NinjectKernel.Get<ILogService>();
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(IsSelected)} has changed...");
                    if (_isSelected == value)
                    {
                        _logService.Trace($"Value of {nameof(IsSelected)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(IsSelected)} to {value}...");
                    _isSelected = value;

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

        protected void OnError(Exception error)
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

                    return;
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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
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

        public string Path
        {
            get => _path;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(Path)} has changed...");
                    if (_path == value)
                    {
                        _logService.Trace($"Value of {nameof(Path)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(Path)} to {value}...");
                    _path = value;

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

        private readonly IDialogService _dialogService;
        private bool _isSelected;
        private readonly ILogService _logService;
        private string _path;

        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

    }
}
