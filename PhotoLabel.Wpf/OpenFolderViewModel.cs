using System;
using PhotoLabel.DependencyInjection;
using PhotoLabel.Services;
using PhotoLabel.Services.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using PhotoLabel.Wpf.Properties;

namespace PhotoLabel.Wpf
{
    public class OpenFolderViewModel : INotifyPropertyChanged
    {
        public OpenFolderViewModel(
            Folder folder,
            IDialogService dialogService,
            ILogService logService)
        {
            // save dependencies
            _dialogService = dialogService;
            _logService = logService;

            // initialise variables
            _includeSubFolders = true;
            SubFolders = CreateSubFolders(folder);
        }

        private ObservableCollection<SubFolderViewModel> CreateSubFolders(Folder folder)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Creating observable collection...");
                var observableCollection = new ObservableCollection<SubFolderViewModel>();

                _logService.Trace($"Creating {folder.SubFolders.Count} subfolders...");
                foreach (var subfolder in folder.SubFolders)
                {
                    var subFolderViewModel = NinjectKernel.Get<SubFolderViewModel>();
                    subFolderViewModel.Path = subfolder.Path;
                    subFolderViewModel.IsSelected = true;

                    observableCollection.Add(subFolderViewModel);
                }

                return observableCollection;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void Deselect()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Deselecting {SubFolders.Count} subfolders...");
                foreach (var subfolder in SubFolders) subfolder.IsSelected = false;
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

        public ICommand DeselectCommand => _deselectCommand ?? (_deselectCommand = new CommandHandler(Deselect, SelectEnabled));

        public bool IncludeSubFolders
        {
            get => _includeSubFolders;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(IncludeSubFolders)} has changed...");
                    if (_includeSubFolders == value)
                    {
                        _logService.Trace($"Value of {nameof(IncludeSubFolders)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(IncludeSubFolders)} to {value}...");
                    _includeSubFolders = value;

                    OnPropertyChanged();
                    (_deselectCommand as ICommandHandler)?.Notify();
                    (_selectCommand as ICommandHandler)?.Notify();
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

        private void Ok(Window window)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Setting the dialog result...");
                window.DialogResult = true;

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

        public ICommand OkCommand => _okCommand ?? (_okCommand = new CommandHandler<Window>(Ok, true));


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

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
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

        private void Select()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Selecting {SubFolders.Count} subfolders...");
                foreach (var subfolder in SubFolders) subfolder.IsSelected = true;
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

        public ICommand SelectCommand => _selectCommand ?? (_selectCommand = new CommandHandler(Select, SelectEnabled));

        private bool SelectEnabled()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if user can select subfolders...");
                return IncludeSubFolders;
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

        public ObservableCollection<SubFolderViewModel> SubFolders { get; }

        public string Title => $"{Properties.Resources.ApplicationName} - [Open]";

        #region delegates
        private delegate void OnErrorDelegate(Exception error);
        private delegate void OnPropertyChangedDelegate(string propertyName);
        #endregion

        #region variables

        private ICommand _deselectCommand;
        private readonly IDialogService _dialogService;
        private bool _includeSubFolders;
        private readonly ILogService _logService;
        private ICommand _okCommand;
        private ICommand _selectCommand;

        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}