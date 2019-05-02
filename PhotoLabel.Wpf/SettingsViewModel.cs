using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using PhotoLabel.DependencyInjection;
using PhotoLabel.Services;
using PhotoLabel.Wpf.Annotations;
using PhotoLabel.Wpf.Properties;

namespace PhotoLabel.Wpf
{
    public class SettingsViewModel : INotifyPropertyChanged, IObservable
    {
        public SettingsViewModel(
            IConfigurationService configurationService,
            ILogService logService)
        {
            // save dependencies
            _configurationService = configurationService;
            _logService = logService;

            // initialise variables
            _observers = new List<IObserver>();

            // get the maximum file size
            GetMaximumFileSize();
        }

        private void Apply()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Saving maximum file size...");
                SetMaximumFileSize();

                _logService.Trace("Flagging that updates have been saved...");
                IsEdited = false;
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

        public ICommand ApplyCommand => _applyCommand ?? (_applyCommand = new CommandHandler(Apply, SaveEnabled));

        private void Close(Window window)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Flagging that close is forced...");
                _forceClose = true;

                _logService.Trace("Closing window...");
                window.Close();
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

        public ICommand CloseCommand => _closeCommand ?? (_closeCommand = new CommandHandler<Window>(Close, true));

        public void Closing(object sender, CancelEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if close is forced...");
                if (_forceClose)
                {
                    _logService.Trace("Close is forced.  Exiting...");
                    return;
                }

                _logService.Trace("Checking if there are unsaved changes...");
                if (!IsEdited)
                {
                    _logService.Trace("There are no unsaved changes.  Exiting...");
                    return;
                }

                _logService.Trace("Prompting for confirmation...");
                if (MessageBox.Show("You have unsaved changes.  Are you sure you want to exit?", "Unsaved Changes",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    e.Cancel = true;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public bool IsEdited
        {
            get => _isEdited;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(IsEdited)} has changed...");
                    if (_isEdited == value)
                    {
                        _logService.Trace($"Value of {nameof(IsEdited)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(IsEdited)} to {value}...");
                    _isEdited = value;

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Title));

                    (_applyCommand as ICommandHandler)?.Notify();
                    (_okCommand as ICommandHandler)?.Notify();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        private void Ok(Window window)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Saving maximum file size...");
                SetMaximumFileSize();

                _logService.Trace("Flagging that close is forced...");
                _forceClose = true;

                _logService.Trace("Closing window...");
                window.Close();
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

        public ICommand OkCommand => _okCommand ?? (_okCommand = new CommandHandler<Window>(Ok, SaveEnabled));

        private bool SaveEnabled()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if edits have been made...");
                return _isEdited;
            }
            catch (Exception ex)
            {
                OnError(ex);

                return false;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OnError(Exception ex)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    _logService.Trace("Not running on UI thread.  Dispatching to UI thread...");
                    Application.Current?.Dispatcher.Invoke(new OnErrorDelegate(OnError), ex);

                    return;
                }

                // log the error
                _logService.Error(ex);

                // let the user know
                MessageBox.Show(Properties.Resources.ErrorText, Resources.ErrorCaption, MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                _logService.TraceEnter();
            }
        }

        public bool MaximumFileSizeEnabled
        {
            get => _maximumFileSizeEnabled;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(MaximumFileSizeEnabled)} has changed...");
                    if (_maximumFileSizeEnabled == value)
                    {
                        _logService.Trace($"Value of {nameof(MaximumFileSizeEnabled)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(MaximumFileSizeEnabled)} to {value}...");
                    _maximumFileSizeEnabled = value;

                    _logService.Trace("Flagging that model has been edited...");
                    IsEdited = true;

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
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

        public int Quantity
        {
            get => _quantity;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(Quantity)} has changed...");
                    if (_quantity == value)
                    {
                        _logService.Trace($"Value of {nameof(Quantity)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(Quantity)} to {value}...");
                    _quantity = value;

                    _logService.Trace("Flagging that model has been edited...");
                    IsEdited = true;

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

        private void GetMaximumFileSize()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if a maximum file size has been set...");
                if (_configurationService.MaxImageSize == null)
                {
                    _logService.Trace("No maximum file size has been set...");
                    _quantity = 1;
                    _maximumFileSizeEnabled = false;
                    _type = QuantityType.Kb;

                    return;
                }

                _logService.Trace("Maximum file size has been set...");
                _maximumFileSizeEnabled = true;

                _logService.Trace($"Reducing {_configurationService.MaxImageSize} to smallest quantity...");
                if (_configurationService.MaxImageSize < 1048576)
                {
                    _quantity = (int)_configurationService.MaxImageSize.Value;

                    return;
                }

                _logService.Trace("Megabytes have been selected...");
                _quantity = (int)_configurationService.MaxImageSize.Value / 1048576;
                _type = QuantityType.Mb;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void SetMaximumFileSize()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if maximum file size is set...");
                if (_maximumFileSizeEnabled)
                {
                    _logService.Trace("Setting maximum file size...");
                    var multiplier = (_type == QuantityType.Kb) ? 1024 : 1048576;
                    _configurationService.MaxImageSize = (ulong) (_quantity * multiplier);

                    return;
                }

                _logService.Trace("Clearing maximum file size...");
                _configurationService.MaxImageSize = null;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public string Title => $"Photo Label - [Settings{(IsEdited?"*":"")}]";

        public QuantityType Type
        {
            get => _type;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(Type)} has changed...");
                    if (_type == value)
                    {
                        _logService.Trace($"Value of {nameof(Type)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(Type)}...");
                    _type = value;

                    _logService.Trace("Flagging that model has been edited...");
                    IsEdited = true;

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

        private delegate void OnErrorDelegate(Exception ex);
        private delegate void OnPropertyChangedDelegate(string propertyName);
        #endregion

        #region enumerations

        public enum QuantityType
        {
            Kb,
            Mb
        }
        #endregion

        #region variables

        private ICommand _applyCommand;
        private ICommand _closeCommand;
        private readonly IConfigurationService _configurationService;
        private bool _forceClose;
        private bool _isEdited;
        private readonly ILogService _logService;
        private ICommand _okCommand;
        private bool _maximumFileSizeEnabled;
        private readonly IList<IObserver> _observers;
        private int _quantity;
        private QuantityType _type;
        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region IObservable

        public IDisposable Subscribe(IObserver observer)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if observer is already subscribed...");
                if (_observers.Contains(observer))
                {
                    _logService.Trace("Observer is already subscribed.  Returning...");
                    return new Subscriber(_observers, observer);
                }

                _logService.Trace("Adding observer...");
                _observers.Add(observer);

                return new Subscriber(_observers, observer);
            }
            finally
            {
                _logService.TraceExit();
            }
        }
        #endregion
    }
}