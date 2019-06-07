using PhotoLabel.Services;
using PhotoLabel.Wpf.Annotations;
using Shared;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

using SystemFonts = System.Drawing.SystemFonts;

namespace PhotoLabel.Wpf
{
    public class SaveAllViewModel : INotifyPropertyChanged
    {
        #region delegates
        private delegate void OnErrorDelegate(Exception ex);
        private delegate void OnPropertyChangedDelegate(string propertyName);
        #endregion

        #region events
        #endregion

        #region variables

        private bool _changeFont;
        private readonly IDialogService _dialogService;
        private string _fontFamily;
        private readonly ILogger _logger;
        private ICommand _okCommand;
        #endregion

        public SaveAllViewModel(
            string directoryPath,
            IDialogService dialogService,
            ILogger logger)
        {
            // save the dependencies
            _dialogService = dialogService;
            _logger = logger;

            // initialise the subfolders
            SubFolders = LoadSubFolders(directoryPath);
        }

        public bool ChangeFont
        {
            get => _changeFont;
            set {
                using (var logger = _logger.Block())
                {
                    logger.Trace($"Checking if the value of {nameof(ChangeFont)} has changed...");
                    if (_changeFont == value)
                    {
                        logger.Trace($"The value of {nameof(ChangeFont)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($@"Setting value of {nameof(ChangeFont)} to ""{value}""...");
                    _changeFont = value;

                    OnPropertyChanged();
                }
            }
        }

        public string FontFamily
        {
            get => _fontFamily ?? SystemFonts.DefaultFont.Name;
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($"Checking if value of {nameof(FontFamily)} has changed...");
                    if (_fontFamily == value)
                    {
                        logger.Trace($"Value of {nameof(FontFamily)} has not changed.  Exiting...");

                        return;
                    }

                    logger.Trace($@"Setting value of {nameof(FontFamily)} to ""{value}""...");
                    _fontFamily = value;

                    OnPropertyChanged();
                }
            }
        }

        private bool IsAFolderSelected(FolderViewModel folderViewModel)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($@"Checking if ""{folderViewModel.Path}"" is selected...");
                if (folderViewModel.IsSelected)
                {
                    logger.Trace($@"""{folderViewModel.Path}"" is selected.  Returning...");
                    return true;
                }

                logger.Trace($@"""{folderViewModel.Path}"" is not selected.  Checking if subfolders are selected...");
                foreach (var subFolderViewModel in folderViewModel.SubFolders)
                {
                    if (IsAFolderSelected(subFolderViewModel)) return true;
                }

                return false;
            }
        }

        private ObservableCollection<FolderViewModel> LoadSubFolders(string directoryPath)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Creating list to return...");
                var treeViewItems = new ObservableCollection<FolderViewModel>();

                logger.Trace($@"Adding ""{directoryPath}"" to list of folders...");
                var folderViewModel = Injector.Get<FolderViewModel>();
                folderViewModel.Path = directoryPath;
                folderViewModel.IsSelected = true;
                folderViewModel.LoadSubFolders();
                treeViewItems.Add(folderViewModel);

                logger.Trace("Watching for changes to folder...");
                folderViewModel.PropertyChanged += FolderViewModel_PropertyChanged;
                return treeViewItems;
            }
        }

        private void FolderViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Bubbling up collection change...");
                OnPropertyChanged(nameof(SubFolders));
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
                    logger.Trace("Checking if any folders have been selected...");
                    return SubFolders.Count == 1 && IsAFolderSelected(SubFolders[0]);
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
                    _dialogService.Error(Properties.Resources.ErrorText);
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
                logger.Trace("Checking if running on the UI thread...");
                if (Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    logger.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Application.Current.Dispatcher.Invoke(new OnPropertyChangedDelegate(OnPropertyChanged),
                        propertyName);

                    return;
                }

                logger.Trace("Firing event...");
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ObservableCollection<FolderViewModel> SubFolders { get; }

        public string Title => $"{Properties.Resources.ApplicationName} - [Save Again]";

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}
