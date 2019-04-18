using PhotoLabel.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using DialogResult = System.Windows.Forms.DialogResult;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace PhotoLabel.Wpf
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region delegates

        private delegate void OnPropertyChangedDelegate(string propertyName);

        private delegate void OpenSuccessDelegate(Task<IList<ImageViewModel>> task, object state);
        #endregion

        #region events

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region variables

        private ICommand _exitCommand;
        private ImageViewModel _imageViewModelToUnload;
        private readonly ILogService _logService;
        private CancellationTokenSource _openCancellationTokenSource;
        private ICommand _openCommand;
        private int _progress;
        private ICommand _scrollCommand;
        private ImageViewModel _selectedImageViewModel;
        #endregion

        public MainWindowViewModel(
            ILogService logService)
        {
            // save dependencies
            _logService = logService;

            // initialise variables
            Images = new ObservableCollection<ImageViewModel>();
        }

        private void Exit()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Cancelling any in progress background tasks...");
                _openCancellationTokenSource?.Cancel();

                _logService.Trace("Stopping application...");
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Properties.Resources.ErrorText, Properties.Resources.ErrorCaption, MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public ICommand ExitCommand => _exitCommand ?? (_exitCommand = new CommandHandler(Exit, true));

        public ObservableCollection<ImageViewModel> Images { get; }

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
                    Application.Current.Dispatcher.Invoke(new OnPropertyChangedDelegate(OnPropertyChanged), DispatcherPriority.Background,
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

        private void Open()
        {
            _logService.TraceEnter();
            try
            {
                using (var folderBrowseDialog = new FolderBrowserDialog())
                {
                    _logService.Trace("Showing folder browse dialog...");
                    folderBrowseDialog.Description = @"Where are the photos?";
                    if (folderBrowseDialog.ShowDialog() != DialogResult.OK) return;

                    // get the selected path
                    var selectedPath = folderBrowseDialog.SelectedPath;

                    _logService.Trace("Creating progress handler...");
                    var progress = new Progress<int>(OpenProgress);

                    _logService.Trace($@"Opening ""{folderBrowseDialog.SelectedPath}"" on background thread...");
                    _openCancellationTokenSource?.Cancel();
                    _openCancellationTokenSource = new CancellationTokenSource();
                    Task<IList<ImageViewModel>>.Factory.StartNew(
                            () => OpenThread(selectedPath, progress,
                                _openCancellationTokenSource.Token),
                            _openCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                        .ContinueWith(OpenSuccess, _openCancellationTokenSource.Token,
                            _openCancellationTokenSource.Token,
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning,
                            TaskScheduler.Default);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public ICommand OpenCommand => _openCommand ?? (_openCommand = new CommandHandler(Open, true));

        private void OpenProgress(int progress)
        {
            _logService.TraceEnter();
            try
            {
                Progress = progress;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OpenSuccess(Task<IList<ImageViewModel>> task, object state)
        {
            var cancellationToken = (CancellationToken) state;

            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace("Checking if running on UI thread...");
                if (Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                        new OpenSuccessDelegate(OpenSuccess),
                        task, state);

                    return;
                }

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($"Adding {task.Result.Count} images...");
                foreach (var imageViewModel in task.Result) Images.Add(imageViewModel);
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private static IList<ImageViewModel> OpenThread(string filename, IProgress<int> progress, CancellationToken cancellationToken)
        {
            var imageService = NinjectKernel.Get<IImageService>();
            var logService = NinjectKernel.Get<ILogService>();

            var stopWatch = Stopwatch.StartNew();

            logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return null;
                logService.Trace("Creating object to return...");
                var imageViewModels = new List<ImageViewModel>();

                if (cancellationToken.IsCancellationRequested) return null;
                logService.Trace($@"Finding image files in ""{filename}""...");
                var imageFilenames = imageService.Find(filename);

                for (var p = 0; p < imageFilenames.Count; p++)
                {
                    if (cancellationToken.IsCancellationRequested) return null;
                    logService.Trace($"Updating progress for image at position {p}...");
                    var percent = (int) (p / (double) imageFilenames.Count * 100d) + 1;
                    progress.Report(percent);

                    logService.Trace($@"Adding ""{filename}"" to list of images...");
                    if (cancellationToken.IsCancellationRequested) return null;
                    imageViewModels.Add(new ImageViewModel(imageFilenames[p]));
                }

                return imageViewModels;
            }
            finally
            {
                logService.TraceExit(stopWatch);
            }
        }

        public int Progress
        {
            get => _progress;
            set
            {
                var logService = NinjectKernel.Get<ILogService>();

                logService.TraceEnter();
                try
                {
                    logService.Trace($"Checking if value of {nameof(Progress)} has changed...");
                    if (_progress == value)
                    {
                        logService.Trace($"Value of {nameof(Progress)} has not changed.  Exiting...");
                        return;
                    }

                    logService.Trace($@"Setting value of {nameof(Progress)} to ""{value}""...");
                    _progress = value;

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProgressVisible));
                }
                finally
                {
                    logService.TraceExit();
                }
            }
        }

        public bool ProgressVisible => _progress > 0 && _progress < 100;

        private void Scroll()
        {
            _logService.TraceEnter();
            try
            {

            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public ICommand ScrollCommand => _scrollCommand ?? (_scrollCommand = new CommandHandler(Scroll, true));

        public ImageViewModel SelectedImageViewModel
        {
            get => _selectedImageViewModel;
            set
            {
                _logService.TraceEnter();
                try
                {
                    // release the second last selected image
                    if (_imageViewModelToUnload != null && _imageViewModelToUnload.Filename != value?.Filename)
                    {
                        _logService.Trace($@"Releasing image for ""{_imageViewModelToUnload.Filename}""...");
                        _imageViewModelToUnload.UnloadImage();
                    }

                    if (_selectedImageViewModel != null)
                    {
                        _logService.Trace($@"Saving ""{_selectedImageViewModel.Filename}"" as image to be unloaded...");
                        _imageViewModelToUnload = _selectedImageViewModel;
                    }

                    _logService.Trace($"Setting value of {nameof(SelectedImageViewModel)}...");
                    _selectedImageViewModel = value;

                    if (_selectedImageViewModel != null)
                    {
                        _logService.Trace($@"Loading ""{_selectedImageViewModel.Filename}""...");
                        _selectedImageViewModel?.LoadMetadata(_openCancellationTokenSource.Token);
                        _selectedImageViewModel?.LoadImage(_openCancellationTokenSource.Token);
                    }

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public string Title => Properties.Resources.ApplicationName;
    }
}