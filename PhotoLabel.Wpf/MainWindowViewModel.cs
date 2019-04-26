﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using PhotoLabel.Services;
using PhotoLabel.Services.Models;
using PhotoLabel.Wpf.Properties;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace PhotoLabel.Wpf
{
    public class MainWindowViewModel : IDisposable, INotifyPropertyChanged, IObservable, IObserver,
        IRecentlyUsedDirectoriesObserver
    {
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
            _taskScheduler = taskScheduler;

            // initialise variables
            Images = new ObservableCollection<ImageViewModel>();
            _observers = new List<IObserver>();
            RecentlyUsedDirectories = new ObservableCollection<RecentlyUsedDirectoryViewModel>();
            _selectedIndex = -1;

            // load the recently used directories
            _recentlyUsedDirectoriesCancellationTokenSource = new CancellationTokenSource();
            recentlyUsedDirectoriesService.Subscribe(this);
            recentlyUsedDirectoriesService.Load(_recentlyUsedDirectoriesCancellationTokenSource.Token);

            // automatically open the last used directory
            OpenLastUsedDirectory();
        }

        public ICommand CloseCommand => _closeCommand ?? (_closeCommand = new CommandHandler(Close, true));

        public ICommand ExitCommand => _exitCommand ?? (_exitCommand = new CommandHandler(Exit, true));

        public bool HasDateTaken => SelectedImageViewModel?.HasDateTaken ?? false;

        public bool HasRecentlyUsedDirectories => RecentlyUsedDirectories.Count > 0;

        public bool HasStatus => Images.Any();

        public ObservableCollection<ImageViewModel> Images { get; }

        public ICommand NextCommand => _nextCommand ?? (_nextCommand = new CommandHandler(Next, true));

        public ICommand OpenRecentlyUsedDirectoryCommand => _openRecentlyUsedDirectoryCommand ??
                                                            (_openRecentlyUsedDirectoryCommand =
                                                                new CommandHandler<string>(OpenDirectory, true));

        public string OutputPath
        {
            get => _outputPath ?? _configurationService.OutputPath;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.TraceEnter($"Checking if value of {nameof(OutputPath)} has changed...");
                    if (_outputPath == value)
                    {
                        _logService.Trace($"Value of {nameof(OutputPath)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($@"Setting value of {nameof(OutputPath)} to ""{value}""...");
                    _outputPath = value;

                    _logService.Trace($@"Setting ""{value}"" as the default output path...");
                    _configurationService.OutputPath = value;

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
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

        public IList<string> QuickCaptions => Images
            .Where(i => !string.IsNullOrWhiteSpace(i.Caption) && !string.IsNullOrWhiteSpace(i.DateTaken) &&
                        i.Caption != SelectedImageViewModel?.Caption &&
                        i.DateTaken == SelectedImageViewModel?.DateTaken).OrderBy(i => i.Caption)
            .Select(i => i.Caption.Replace("_", "__")).ToList();

        public ObservableCollection<RecentlyUsedDirectoryViewModel> RecentlyUsedDirectories { get; }

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
                        _imageViewModelToUnload.PropertyChanged -= ImageViewModel_PropertyChanged;
                    }

                    _logService.Trace($"Setting value of {nameof(SelectedImageViewModel)}...");
                    _selectedImageViewModel = value;

                    if (value != null)
                    {
                        _logService.Trace($@"Saving ""{value.Filename}"" as image to be unloaded...");
                        _imageViewModelToUnload = value;

                        _logService.Trace($@"Loading ""{value.Filename}""...");
                        value.LoadImage(_openCancellationTokenSource.Token);
                        value.LoadPreview(false, _openCancellationTokenSource.Token);

                        _logService.Trace($@"Saving ""{value.Filename}"" as last selected image...");
                        _recentlyUsedDirectoriesService.SetLastSelectedFile(value.Filename);

                        // watch for changes to the date taken
                        value.PropertyChanged += ImageViewModel_PropertyChanged;
                    }

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasDateTaken));
                    OnPropertyChanged(nameof(QuickCaptions));
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public int SelectedIndex
        {
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

                    _logService.Trace(@"Checking if there is a next image...");
                    if (_selectedIndex < Images.Count - 1)
                    {
                        _logService.Trace($@"Preloading image at position {_selectedIndex + 1}...");
                        Images[_selectedIndex + 1].LoadImage(_openCancellationTokenSource.Token);
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

        public string Title => Resources.ApplicationName;

        public WindowState WindowState
        {
            get
            {
                switch (_configurationService.WindowState)
                {
                    case FormWindowState.Maximized:
                        return WindowState.Maximized;
                    case FormWindowState.Minimized:
                        return WindowState.Minimized;
                    default:
                        return WindowState.Normal;
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
                            _configurationService.WindowState = FormWindowState.Maximized;

                            break;
                        case WindowState.Minimized:
                            _configurationService.WindowState = FormWindowState.Minimized;

                            break;
                        default:
                            _configurationService.WindowState = FormWindowState.Normal;

                            break;
                    }

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

        private void Close()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Cancelling all background tasks...");
                _openCancellationTokenSource?.Cancel();
                _recentlyUsedDirectoriesCancellationTokenSource?.Cancel();

                _logService.Trace("Clearing list of images...");
                Images.Clear();

                OnPropertyChanged(nameof(HasDateTaken));
                OnPropertyChanged(nameof(HasStatus));
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                OnError(ex);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void Exit()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Cancelling any in progress background tasks...");
                _openCancellationTokenSource?.Cancel();
                _recentlyUsedDirectoriesCancellationTokenSource.Cancel();
                _taskScheduler.Dispose();

                _logService.Trace("Stopping application...");
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                _logService.Error(ex);

                MessageBox.Show(Resources.ErrorText, Resources.ErrorCaption, MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void Next()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if there is an image to move to...");
                if (_selectedIndex == Images.Count - 1)
                {
                    _logService.Trace("There is no image to move to.  Exiting...");
                    return;
                }

                _logService.Trace($"Moving to image at position {_selectedIndex + 1}...");
                SelectedImageViewModel = Images[_selectedIndex + 1];
            }
            finally
            {
                _logService.TraceExit();
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
                _openCancellationTokenSource?.Cancel();
                _openCancellationTokenSource = new CancellationTokenSource();
                new Thread(OpenDirectoryThread).Start(new object[]
                    {directory, progress, _openCancellationTokenSource.Token});
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private void OpenDirectoryThread(object state)
        {
            var stateArray = (object[]) state;
            var filename = (string) stateArray[0];
            var progress = (IProgress<int>) stateArray[1];
            var cancellationToken = (CancellationToken) stateArray[2];

            // create dependencies
            var imageService = NinjectKernel.Get<IImageService>();
            var logService = NinjectKernel.Get<ILogService>();
            var recentlyUsedDirectoriesService = NinjectKernel.Get<IRecentlyUsedDirectoriesService>();

            var stopWatch = Stopwatch.StartNew();

            logService.TraceEnter();
            try
            {
                var imageViewModels = new List<ImageViewModel>();

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace("Clearing any in progress preview loads...");
                _taskScheduler.Clear();

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($@"Finding image files in ""{filename}""...");
                var imageFilenames = imageService.Find(filename);

                // load the images backwards to queue the last image first
                for (var p = imageFilenames.Count; p > 0;)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace($"Updating progress for image at position {p}...");
                    var percent = (int) ((imageFilenames.Count - p + 1) / (double) imageFilenames.Count * 100d);
                    progress.Report(percent);

                    if (cancellationToken.IsCancellationRequested) return;
                    var imageFilename = imageFilenames[--p];
                    logService.Trace($@"Creating image view model for ""{imageFilename}""...");
                    var imageViewModel = new ImageViewModel(imageFilename);
                    imageViewModel.Subscribe(this);

                    logService.Trace($@"Adding ""{filename}"" to list of images...");
                    if (cancellationToken.IsCancellationRequested) return;
                    imageViewModels.Insert(0, imageViewModel);
                }

                logService.Trace($@"Adding ""{filename}"" to recently used directories...");
                recentlyUsedDirectoriesService.Add(filename);

                if (cancellationToken.IsCancellationRequested) return;
                UpdateImages(imageViewModels);

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace("Getting most recently used image...");
                var mostRecentlyUsedImageViewModelFilename = _recentlyUsedDirectoriesService.GetMostRecentlyUsedFile();
                if (mostRecentlyUsedImageViewModelFilename == null)
                {
                    logService.Trace("No recently used image found.  Exiting...");
                    return;
                }

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($@"Finding image view model ""{mostRecentlyUsedImageViewModelFilename}""...");
                var selectedImageViewModel =
                    imageViewModels.FirstOrDefault(i => i.Filename == mostRecentlyUsedImageViewModelFilename);
                if (selectedImageViewModel == null)
                {
                    logService.Trace(
                        $@"""{mostRecentlyUsedImageViewModelFilename}"" not found in directory.  Exiting...");
                    return;
                }

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace(
                    $@"Setting selected image to ""{mostRecentlyUsedImageViewModelFilename}"" on UI thread...");
                Application.Current?.Dispatcher.Invoke(() => SelectedImageViewModel = selectedImageViewModel);
            }
            finally
            {
                logService.TraceExit(stopWatch);
            }
        }

        public bool Save(string outputPath)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Saving ""{outputPath}"" as the default output path...");
                OutputPath = outputPath;

                _logService.Trace("Checking if there is a selected image...");
                if (_selectedImageViewModel == null)
                {
                    _logService.Trace("There is no selected image.  Exiting...");
                    return true;
                }

                _logService.Trace($@"Saving ""{_selectedImageViewModel.Filename}""...");
                _selectedImageViewModel.Save(outputPath, false);

                _logService.Trace("Checking if there is an image to move to...");
                if (_selectedIndex >= Images.Count - 1)
                {
                    _logService.Trace("There is no image to move to.  Exiting...");
                    return true;
                }

                _logService.Trace($"Moving to image at position {_selectedIndex + 1}...");
                SelectedImageViewModel = Images[_selectedIndex + 1];
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                _logService.TraceExit();
            }

            return true;
        }

        private void ImageViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Changed property is ""{e.PropertyName}"".  Handling change...");
                switch (e.PropertyName)
                {
                    case "DateTaken":
                        OnPropertyChanged(nameof(QuickCaptions));

                        break;
                    case "HasDateTaken":
                        OnPropertyChanged(nameof(HasDateTaken));

                        break;
                }
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
                    Application.Current?.Dispatcher.Invoke(new UpdateImagesDelegate(UpdateImages),
                        DispatcherPriority.Background, imageViewModels);

                    return;
                }

                logService.Trace("Clearing current images...");
                Images.Clear();

                logService.Trace($"Adding {imageViewModels.Count} images on UI thread...");
                foreach (var imageViewModel in imageViewModels) Images.Add(imageViewModel);

                OnPropertyChanged(nameof(HasStatus));
            }
            finally
            {
                logService.TraceExit();
            }
        }

        #region delegates

        private delegate void OnClearDelegate();

        private delegate void OnErrorDelegate(Exception error);
        private delegate void OnNextDelegate(Directory directory);

        private delegate void OnPropertyChangedDelegate(string propertyName);

        private delegate void UpdateImagesDelegate(List<ImageViewModel> images);

        #endregion

        #region variables

        private ICommand _closeCommand;
        private readonly IConfigurationService _configurationService;
        private ICommand _exitCommand;
        private ImageViewModel _imageViewModelToUnload;
        private readonly ILogService _logService;
        private ICommand _nextCommand;
        private readonly IList<IObserver> _observers;
        private ICommand _openRecentlyUsedDirectoryCommand;
        private string _outputPath;
        private int _progress;
        private readonly IRecentlyUsedDirectoriesService _recentlyUsedDirectoriesService;
        private ImageViewModel _selectedImageViewModel;
        private int _selectedIndex;
        private CancellationTokenSource _openCancellationTokenSource;
        private readonly SingleTaskScheduler _taskScheduler;
        private bool _disposedValue; // To detect redundant calls
        private readonly CancellationTokenSource _recentlyUsedDirectoriesCancellationTokenSource;

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                // cancel any in progress load
                _openCancellationTokenSource?.Cancel();

                // dispose of dependent objects
                _taskScheduler.Dispose();

                foreach (var imageViewModel in Images) imageViewModel.Dispose();
            }

            _disposedValue = true;
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion

        #region  INotifyPropertyChanged
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

        #region IRecentlyUsedDirectoriesObserver

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

                logService.Trace($"Notifying {_observers.Count} observers of error...");
                foreach (var observer in _observers) observer.OnError(error);
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