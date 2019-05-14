using PhotoLabel.Services;
using PhotoLabel.Wpf.Annotations;
using PhotoLabel.Wpf.Properties;
using Shared;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PhotoLabel.Wpf
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public SettingsViewModel(
            IConfigurationService configurationService,
            IDialogService dialogService,
            ILogger logger)
        {
            // save dependencies
            _configurationService = configurationService;
            _dialogService = dialogService;
            _logger = logger;

            // get the maximum file size
            GetMaximumFileSize();
        }

        private void Apply()
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace("Saving maximum file size...");
                    SetMaximumFileSize();

                    logger.Trace("Flagging that updates have been saved...");
                    IsEdited = false;
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public ICommand ApplyCommand => _applyCommand ?? (_applyCommand = new CommandHandler(Apply, SaveEnabled));

        private void Close(Window window)
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace("Flagging that close is forced...");
                    _forceClose = true;

                    logger.Trace("Closing window...");
                    window.Close();
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public ICommand CloseCommand => _closeCommand ?? (_closeCommand = new CommandHandler<Window>(Close, true));

        public void Closing(object sender, CancelEventArgs e)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Checking if close is forced...");
                if (_forceClose)
                {
                    logger.Trace("Close is forced.  Exiting...");
                    return;
                }

                logger.Trace("Checking if there are unsaved changes...");
                if (!IsEdited)
                {
                    logger.Trace("There are no unsaved changes.  Exiting...");
                    return;
                }

                logger.Trace("Prompting for confirmation...");
                e.Cancel = !_dialogService.Confirm("You have unsaved changes.  Do you wish to exit?",
                    "Unsaved Changes");
            }
        }

        public bool IsEdited
        {
            get => _isEdited;
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($"Checking if value of {nameof(IsEdited)} has changed...");
                    if (_isEdited == value)
                    {
                        logger.Trace($"Value of {nameof(IsEdited)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($"Setting value of {nameof(IsEdited)} to {value}...");
                    _isEdited = value;

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Title));

                    (_applyCommand as ICommandHandler)?.Notify();
                    (_okCommand as ICommandHandler)?.Notify();
                }
            }
        }

        private void Ok(Window window)
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace("Saving maximum file size...");
                    SetMaximumFileSize();

                    logger.Trace("Flagging that close is forced...");
                    _forceClose = true;

                    logger.Trace("Closing window...");
                    window.Close();
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public ICommand OkCommand => _okCommand ?? (_okCommand = new CommandHandler<Window>(Ok, SaveEnabled));

        private bool SaveEnabled()
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace("Checking if edits have been made...");
                    return _isEdited;
                }
                catch (Exception ex)
                {
                    OnError(ex);

                    return false;
                }
            }
        }

        private void OnError(Exception ex)
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace("Checking if running on UI thread...");
                    if (Application.Current?.Dispatcher.CheckAccess() == false)
                    {
                        logger.Trace("Not running on UI thread.  Dispatching to UI thread...");
                        Application.Current?.Dispatcher.Invoke(new OnErrorDelegate(OnError), ex);

                        return;
                    }

                    // log the error
                    logger.Error(ex);

                    // let the user know
                    _dialogService.Error(Resources.ErrorText);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        public bool MaximumFileSizeEnabled
        {
            get => _maximumFileSizeEnabled;
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($"Checking if value of {nameof(MaximumFileSizeEnabled)} has changed...");
                    if (_maximumFileSizeEnabled == value)
                    {
                        logger.Trace($"Value of {nameof(MaximumFileSizeEnabled)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($"Setting value of {nameof(MaximumFileSizeEnabled)} to {value}...");
                    _maximumFileSizeEnabled = value;

                    logger.Trace("Flagging that model has been edited...");
                    IsEdited = true;

                    OnPropertyChanged();
                }
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            using (var logService = _logger.Block())
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
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                using (var logger = _logger.Block())
                {
                    try
                    {
                        logger.Trace($"Checking if value of {nameof(Quantity)} has changed...");
                        if (_quantity == value)
                        {
                            logger.Trace($"Value of {nameof(Quantity)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($"Setting value of {nameof(Quantity)} to {value}...");
                        _quantity = value;

                        logger.Trace("Flagging that model has been edited...");
                        IsEdited = true;

                        OnPropertyChanged();
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                    }
                }
            }
        }

        private void GetMaximumFileSize()
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Checking if a maximum file size has been set...");
                if (_configurationService.MaxImageSize == null)
                {
                    logger.Trace("No maximum file size has been set...");
                    _quantity = 1;
                    _maximumFileSizeEnabled = false;
                    _type = QuantityType.Kb;

                    return;
                }

                logger.Trace("Maximum file size has been set...");
                _maximumFileSizeEnabled = true;

                logger.Trace($"Reducing {_configurationService.MaxImageSize} to smallest quantity...");
                if (_configurationService.MaxImageSize < 1048576)
                {
                    _quantity = (int)_configurationService.MaxImageSize.Value;

                    return;
                }

                logger.Trace("Megabytes have been selected...");
                _quantity = (int)_configurationService.MaxImageSize.Value / 1048576;
                _type = QuantityType.Mb;
            }
        }

        private void SetMaximumFileSize()
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Checking if maximum file size is set...");
                if (_maximumFileSizeEnabled)
                {
                    logger.Trace("Setting maximum file size...");
                    var multiplier = (_type == QuantityType.Kb) ? 1024 : 1048576;
                    _configurationService.MaxImageSize = (ulong)(_quantity * multiplier);

                    return;
                }

                logger.Trace("Clearing maximum file size...");
                _configurationService.MaxImageSize = null;
            }
        }

        public string Title => $"Photo Label - [Settings{(IsEdited ? "*" : "")}]";

        public QuantityType Type
        {
            get => _type;
            set
            {
                using (var logger = _logger.Block())
                {
                    try
                    {
                        logger.Trace($"Checking if value of {nameof(Type)} has changed...");
                        if (_type == value)
                        {
                            logger.Trace($"Value of {nameof(Type)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($"Setting value of {nameof(Type)}...");
                        _type = value;

                        logger.Trace("Flagging that model has been edited...");
                        IsEdited = true;

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
        private readonly IDialogService _dialogService;
        private bool _forceClose;
        private bool _isEdited;
        private readonly ILogger _logger;
        private ICommand _okCommand;
        private bool _maximumFileSizeEnabled;
        private int _quantity;
        private QuantityType _type;

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}