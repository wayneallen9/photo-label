using PhotoLabel.Services;
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
            FolderViewModel folderViewModel,
            IDialogService dialogService,
            ILogger logger)
        {
            // save dependencies
            _dialogService = dialogService;
            _logger = logger;

            // initialise variables
            SubFolders = CreateSubFolders(folderViewModel);

            // manually handle property changes on the folder view model
            folderViewModel.PropertyChanged += FolderViewModel_PropertyChanged;
        }

        private void FolderViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace("Checking command validity...");
                    ((ICommandHandler) OkCommand).Notify();
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        private ObservableCollection<IFolderViewModel> CreateSubFolders(FolderViewModel folder)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Creating observable collection...");
                var observableCollection = new ObservableCollection<IFolderViewModel>();

                logger.Trace($@"Adding ""{folder.Path}"" to observable collection...");
                observableCollection.Add(folder);

                return observableCollection;
            }
        }

        private bool IsAFolderSelected(ObservableCollection<IFolderViewModel> folderViewModels)
        {
            using (var logger = _logger.Block())
            {
                foreach (var folderViewModel in folderViewModels)
                {
                    logger.Trace($@"Checking if ""{folderViewModel.Path}"" is selected...");
                    if (folderViewModel.IsSelected)
                    {
                        logger.Trace($@"""{folderViewModel.Path}"" is selected.  Returning...");
                        return true;
                    }

                    logger.Trace(
                        $@"""{folderViewModel.Path}"" is not selected.  Checking if subfolders are selected...");
                    if (IsAFolderSelected(folderViewModel.SubFolders)) return true;
                }

                return false;
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

        public ICommand OkCommand => _okCommand ?? (_okCommand = new CommandHandler<Window>(Ok, OkEnabled));

        private bool OkEnabled()
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace("Checking if a folder has been selected...");
                    return IsAFolderSelected(SubFolders);
                }
                catch (Exception ex)
                {
                    OnError(ex);

                    return false;
                }
            }
        }

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

        public ObservableCollection<IFolderViewModel> SubFolders { get; }

        public string Title => $"{Resources.ApplicationName} - [Open]";

        #region delegates
        private delegate void OnErrorDelegate(Exception error);
        private delegate void OnPropertyChangedDelegate(string propertyName);
        #endregion

        #region variables

        private readonly IDialogService _dialogService;
        private readonly ILogger _logger;
        private ICommand _okCommand;

        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}