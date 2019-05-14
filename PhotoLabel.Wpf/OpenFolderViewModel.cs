using PhotoLabel.Services;
using PhotoLabel.Services.Models;
using PhotoLabel.Wpf.Properties;
using Shared;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PhotoLabel.Wpf
{
    public class OpenFolderViewModel : INotifyPropertyChanged
    {
        public OpenFolderViewModel(
            Folder folder,
            IDialogService dialogService,
            ILogger logger)
        {
            // save dependencies
            _dialogService = dialogService;
            _logger = logger;

            // initialise variables
            _includeSubFolders = true;
            SubFolders = CreateSubFolders(folder);
        }

        private ObservableCollection<SubFolderViewModel> CreateSubFolders(Folder folder)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Creating observable collection...");
                var observableCollection = new ObservableCollection<SubFolderViewModel>();

                logger.Trace($"Creating {folder.SubFolders.Count} subfolders...");
                foreach (var subfolder in folder.SubFolders)
                {
                    var subFolderViewModel = Injector.Get<SubFolderViewModel>();
                    subFolderViewModel.Path = subfolder.Path;
                    subFolderViewModel.IsSelected = true;

                    observableCollection.Add(subFolderViewModel);
                }

                return observableCollection;
            }
        }

        private void Deselect()
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace($"Deselecting {SubFolders.Count} subfolders...");
                    foreach (var subfolder in SubFolders) subfolder.IsSelected = false;
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public ICommand DeselectCommand => _deselectCommand ?? (_deselectCommand = new CommandHandler(Deselect, SelectEnabled));

        public bool IncludeSubFolders
        {
            get => _includeSubFolders;
            set
            {
                using (var logger = _logger.Block())
                {
                    try
                    {
                        logger.Trace($"Checking if value of {nameof(IncludeSubFolders)} has changed...");
                        if (_includeSubFolders == value)
                        {
                            logger.Trace($"Value of {nameof(IncludeSubFolders)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($"Setting value of {nameof(IncludeSubFolders)} to {value}...");
                        _includeSubFolders = value;

                        OnPropertyChanged();
                        (_deselectCommand as ICommandHandler)?.Notify();
                        (_selectCommand as ICommandHandler)?.Notify();
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                    }
                }
            }
        }

        private void Ok(Window window)
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace("Setting the dialog result...");
                    window.DialogResult = true;

                    logger.Trace("Closing window...");
                    window.Close();
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public ICommand OkCommand => _okCommand ?? (_okCommand = new CommandHandler<Window>(Ok, true));


        protected void OnError(Exception error)
        {
            using (var logService = _logger.Block())
            {
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

                    logService.Trace("Notifying user of error...");
                    _dialogService.Error(Resources.ErrorText);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
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

        private void Select()
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace($"Selecting {SubFolders.Count} subfolders...");
                    foreach (var subfolder in SubFolders) subfolder.IsSelected = true;
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public ICommand SelectCommand => _selectCommand ?? (_selectCommand = new CommandHandler(Select, SelectEnabled));

        private bool SelectEnabled()
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace("Checking if user can select subfolders...");
                    return IncludeSubFolders;
                }
                catch (Exception ex)
                {
                    OnError(ex);
                    return false;
                }
            }
        }

        public ObservableCollection<SubFolderViewModel> SubFolders { get; }

        public string Title => $"{Resources.ApplicationName} - [Open]";

        #region delegates
        private delegate void OnErrorDelegate(Exception error);
        private delegate void OnPropertyChangedDelegate(string propertyName);
        #endregion

        #region variables

        private ICommand _deselectCommand;
        private readonly IDialogService _dialogService;
        private bool _includeSubFolders;
        private readonly ILogger _logger;
        private ICommand _okCommand;
        private ICommand _selectCommand;

        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}