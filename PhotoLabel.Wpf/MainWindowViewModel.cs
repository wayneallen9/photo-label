using PhotoLabel.Services;
using PhotoLabel.Services.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PhotoLabel.Wpf
{
    public class MainWindowViewModel : INotifyPropertyChanged, IRecentlyUsedDirectoriesObserver
    {
        #region delegates

        private delegate void OnClearDelegate();
        private delegate void OnNextDelegate(Directory directory);
        private delegate void OnPropertyChangedDelegate(string propertyName);
        private delegate void OpenSuccessDelegate(Task<IList<ImageViewModel>> task, object state);
        private delegate void UpdateImagesDelegate(List<ImageViewModel> images);
        #endregion

        #region events

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region variables
        private readonly IConfigurationService _configurationService;
        private ICommand _exitCommand;
        private ImageViewModel _imageViewModelToUnload;
        private readonly ILogService _logService;
        private ICommand _openRecentlyUsedDirectoryCommand;
        private int _progress;
        private readonly IRecentlyUsedDirectoriesService _recentlyUsedDirectoriesService;
        private ICommand _scrollCommand;
        private ImageViewModel _selectedImageViewModel;
        private int _selectedIndex;
        #endregion

        public MainWindowViewModel(
            IConfigurationService configurationService,
            ILogService logService,
            IRecentlyUsedDirectoriesService recentlyUsedDirectoriesService,
            SingleTaskScheduler taskScheduler)
        {
            // save dependencies
            _configurationService = configurationService;
            _logService = logService;
            _recentlyUsedDirectoriesService = recentlyUsedDirectoriesService;
            TaskScheduler = taskScheduler;

            // initialise variables
            Images = new ObservableCollection<ImageViewModel>();
            RecentlyUsedDirectories = new ObservableCollection<RecentlyUsedDirectoryViewModel>();
            RecentlyUsedDirectoriesCancellationTokenSource = new CancellationTokenSource();
            _selectedIndex = -1;

            // load the recently used directories
            recentlyUsedDirectoriesService.Subscribe(this);
            recentlyUsedDirectoriesService.Load(RecentlyUsedDirectoriesCancellationTokenSource.Token);

            // automatically open the last used directory
            OpenLastUsedDirectory();
        }

        private void Exit()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Cancelling any in progress background tasks...");
                OpenCancellationTokenSource?.Cancel();
                RecentlyUsedDirectoriesCancellationTokenSource.Cancel();
                TaskScheduler.Dispose();

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

        public bool HasRecentlyUsedDirectories => RecentlyUsedDirectories.Count > 0;

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

        public void Open(string directory)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Opening directory ""{directory}""...");
                OpenDirectory(directory);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private CancellationTokenSource OpenCancellationTokenSource { get; set; }

        private void OpenLastUsedDirectory()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Getting most recently used directory...");
                var mostRecentlyUsedDirectory = _recentlyUsedDirectoriesService.GetMostRecentlyUsedDirectory();
                if (mostRecentlyUsedDirectory == null)
                {
                    _logService.Trace("There is no most recently used directory.  Exiting...");
                    return;
                }

                _logService.Trace($@"Opening directory ""{mostRecentlyUsedDirectory}""...");

                _logService.Trace("Creating progress handler...");
                var progress = new Progress<int>(OpenProgress);

                _logService.Trace($@"Opening ""{mostRecentlyUsedDirectory}"" on background thread...");
                OpenDirectory(mostRecentlyUsedDirectory);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

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

        private void OpenDirectory(string directory)
        {
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                _logService.Trace("Creating progress handler...");
                var progress = new Progress<int>(OpenProgress);

                _logService.Trace($@"Opening ""{directory}"" on background thread...");
                OpenCancellationTokenSource?.Cancel();
                OpenCancellationTokenSource = new CancellationTokenSource();
                new Thread(OpenThread).Start(new object[] { directory, progress, OpenCancellationTokenSource.Token });
            }
            finally
            {
                logService.TraceExit();
            }
        }

        public ICommand OpenRecentlyUsedDirectoryCommand => _openRecentlyUsedDirectoryCommand ?? (_openRecentlyUsedDirectoryCommand = new CommandHandler<string>(OpenDirectory, true));

        private void OpenThread(object state)
        {
            var stateArray = (object[])state;
            var filename = (string)stateArray[0];
            var progress = (IProgress<int>)stateArray[1];
            var cancellationToken = (CancellationToken)stateArray[2];

            // create dependencies
            var imageService = NinjectKernel.Get<IImageService>();
            var logService = NinjectKernel.Get<ILogService>();
            var recentlyUsedDirectoriesService = NinjectKernel.Get<IRecentlyUsedDirectoriesService>();

            var stopWatch = Stopwatch.StartNew();

            logService.TraceEnter();
            try
            {
                var imageViewModels = new List<ImageViewModel>();

                if (cancellationToken.IsCancellationRequested) return ;
                logService.Trace($@"Finding image files in ""{filename}""...");
                var imageFilenames = imageService.Find(filename);

                for (var p = 0; p < imageFilenames.Count; p++)
                {
                    if (cancellationToken.IsCancellationRequested) return ;
                    logService.Trace($"Updating progress for image at position {p}...");
                    var percent = (int) (p / (double) imageFilenames.Count * 100d) + 1;
                    progress.Report(percent);

                    logService.Trace($@"Adding ""{filename}"" to list of images...");
                    if (cancellationToken.IsCancellationRequested) return;
                    imageViewModels.Add(new ImageViewModel(imageFilenames[p]));
                }

                logService.Trace($@"Adding ""{filename}"" to recently used directories...");
                recentlyUsedDirectoriesService.Add(filename);

                if (cancellationToken.IsCancellationRequested) return;
                UpdateImages(imageViewModels);

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($"Getting most recently used image...");
                var mostRecentlyUsedImageViewModelFilename = _recentlyUsedDirectoriesService.GetMostRecentlyUsedFile();
                if (mostRecentlyUsedImageViewModelFilename == null)
                {
                    logService.Trace("No recently used image found.  Exiting...");
                    return;
                }

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($@"Finding image view model ""{mostRecentlyUsedImageViewModelFilename}""...");
                var selectedImageViewModel = imageViewModels.FirstOrDefault(i => i.Filename == mostRecentlyUsedImageViewModelFilename);
                if (selectedImageViewModel == null)
                {
                    logService.Trace($@"""{mostRecentlyUsedImageViewModelFilename}"" not found in directory.  Exiting...");
                    return;
                }

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($@"Setting selected image to ""{mostRecentlyUsedImageViewModelFilename}"" on UI thread...");
                Application.Current?.Dispatcher.Invoke(() => SelectedImageViewModel = selectedImageViewModel);
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

        public ObservableCollection<RecentlyUsedDirectoryViewModel> RecentlyUsedDirectories { get; }

        private CancellationTokenSource RecentlyUsedDirectoriesCancellationTokenSource { get; }

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
                        _selectedImageViewModel?.LoadImage(OpenCancellationTokenSource.Token);
                        _selectedImageViewModel?.LoadPreview(OpenCancellationTokenSource.Token);
                    }

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public int SelectedIndex {
            get => _selectedIndex;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(SelectedIndex)} has changed...");
                    if (_selectedIndex == value)
                    {
                        _logService.Trace($"Value of {nameof(SelectedIndex)} has not changed.  Exiting...");

                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(SelectedIndex)} to {value}...");
                    _selectedIndex = value;

                    _logService.Trace($@"Checking if there is a next image...");
                    if (_selectedIndex < Images.Count - 1)
                    {
                        _logService.Trace($@"Preloading image at position {_selectedIndex + 1}...");
                        Images[_selectedIndex + 1].LoadImage(OpenCancellationTokenSource.Token);
                    }

                    OnPropertyChanged(nameof(Status));
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public string Status => $"{_selectedIndex + 1} of {Images.Count}";

        private SingleTaskScheduler TaskScheduler { get; }

        public string Title => Properties.Resources.ApplicationName;

        private void UpdateImages(List<ImageViewModel> imageViewModels)
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
                    Application.Current?.Dispatcher.Invoke(new UpdateImagesDelegate(UpdateImages), DispatcherPriority.Background, imageViewModels);

                    return;
                }

                logService.Trace("Clearing current images...");
                Images.Clear();

                logService.Trace($"Adding {imageViewModels.Count} images on UI thread...");
                foreach (var imageViewModel in imageViewModels) Images.Add(imageViewModel);
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private WindowState WindowState
        {
            get {
                switch (_configurationService.WindowState) {
                    case System.Windows.Forms.FormWindowState.Maximized:
                        return System.Windows.WindowState.Maximized;
                    case System.Windows.Forms.FormWindowState.Minimized:
                        return System.Windows.WindowState.Minimized;
                    default:
                        return System.Windows.WindowState.Normal;
                }
            }
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace("Setting new window state...");
                    switch (value)
                    {
                        case WindowState.Maximized:
                            _configurationService.WindowState = System.Windows.Forms.FormWindowState.Maximized;

                            break;
                        case WindowState.Minimized:
                            _configurationService.WindowState = System.Windows.Forms.FormWindowState.Minimized;

                            break;
                        default:
                            _configurationService.WindowState = System.Windows.Forms.FormWindowState.Normal;

                            break;
                    }

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        #region IObservable
        public void OnClear()
        {
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace("Checking if running on UI thread...");
                if (Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Application.Current.Dispatcher.Invoke(new OnClearDelegate(OnClear));

                    return;
                }

                logService.Trace("Clearing list of recently used directories...");
                RecentlyUsedDirectories.Clear();
            }
            finally
            {
                logService.TraceExit();
            }
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(Directory directory)
        {
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace("Checking if running on UI thread...");
                if (Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Application.Current.Dispatcher.Invoke(new OnNextDelegate(OnNext), directory);

                    return;
                }

                logService.Trace($@"Copying ""{directory.Path}"" to view model...");
                var viewModel = Mapper.Map<RecentlyUsedDirectoryViewModel>(directory);

                logService.Trace($@"Adding ""{directory.Path}"" to recently used directories...");
                RecentlyUsedDirectories.Add(viewModel);
            }
            finally
            {
                logService.TraceExit();
            }
        }
        #endregion
    }
}