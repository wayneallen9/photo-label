﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Ninject.Parameters;
using PhotoLabel.DependencyInjection;
using PhotoLabel.Services;
using PhotoLabel.Wpf.Properties;
using Xceed.Wpf.Toolkit;
using Application = System.Windows.Application;
using Folder = PhotoLabel.Services.Models.Folder;
using MessageBox = System.Windows.MessageBox;
using WindowState = System.Windows.WindowState;

namespace PhotoLabel.Wpf
{
    public class MainWindowViewModel : IDisposable, INotifyPropertyChanged, IObserver,
        IRecentlyUsedDirectoriesObserver
    {
        public MainWindowViewModel(
            IDialogService dialogService,
            IConfigurationService configurationService,
            IFolderService folderService,
            IImageService imageService,
            ILogService logService,
            INavigationService navigationService,
            IOpacityService opacityService,
            IRecentlyUsedFoldersService recentlyUsedDirectoriesService,
            SingleTaskScheduler taskScheduler,
            IWhereService whereService)
        {
            // save dependencies
            _dialogService = dialogService;
            _configurationService = configurationService;
            _folderService = folderService;
            _imageService = imageService;
            _logService = logService;
            _opacityService = opacityService;
            _navigationService = navigationService;
            _recentlyUsedDirectoriesService = recentlyUsedDirectoriesService;
            _taskScheduler = taskScheduler;
            _whereService = whereService;

            // initialise variables
            Images = new ObservableCollection<ImageViewModel>();
            _observers = new List<IObserver>();
            _recentlyUsedBackColors = LoadRecentlyUsedBackColors();
            RecentlyUsedFolders = new ObservableCollection<FolderViewModel>();
            _selectedIndex = -1;

            // load the recently used directories
            _recentlyUsedDirectoriesCancellationTokenSource = new CancellationTokenSource();
            recentlyUsedDirectoriesService.Subscribe(this);
            recentlyUsedDirectoriesService.Load(_recentlyUsedDirectoriesCancellationTokenSource.Token);

            // automatically open the last used directory
            OpenLastUsedDirectory();
        }

        public string BackColorOpacity
        {
            get => _selectedImageViewModel?.BackColorOpacity ?? _opacityService.GetOpacity(_configurationService.BackgroundColour);
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace("Setting default opacity...");
                    _configurationService.BackgroundColour = _opacityService.SetOpacity(_configurationService.BackgroundColour, value);

                    _logService.Trace("Checking if there is a selected image view model...");
                    if (_selectedImageViewModel != null)
                    {
                        _logService.Trace("Setting selected image opacity...");
                        _selectedImageViewModel.BackColor = _opacityService.SetOpacity(_selectedImageViewModel.BackColor, value);
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

        public double CaptionSize
        {
            get => _configurationService.CaptionSize;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(CaptionSize)} has changed...");
                    if (Math.Abs(_configurationService.CaptionSize - value) < double.Epsilon)
                    {
                        _logService.Trace($"Value of {nameof(CaptionSize)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(CaptionSize)} to {value}...");
                    _configurationService.CaptionSize = value;

                    OnPropertyChanged();
                    (_zoomOutCommand as ICommandHandler)?.Notify();
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

        private void CheckCommandsEnabled()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Notifying commands enabled change...");
                (_deleteCommand as ICommandHandler)?.Notify();
                (_nextCommand as ICommandHandler)?.Notify();
                (_resetBrightnessCommand as ICommandHandler)?.Notify();
                (_rotateLeftCommand as ICommandHandler)?.Notify();
                (_rotateRightCommand as ICommandHandler)?.Notify();
                (_saveAsCommand as ICommandHandler)?.Notify();
                (_saveCommand as ICommandHandler)?.Notify();
                (_whereCommand as ICommandHandler)?.Notify();
            }
            finally
            {
                _logService.TraceExit();
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

                _logService.Trace("Resetting variables...");
                _indexToRemove = 0;
                _nextIndexToRemove = 0;
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

        public ICommand CloseCommand => _closeCommand ?? (_closeCommand = new CommandHandler(Close, true));

        public void Closing(object sender, CancelEventArgs e)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if there are any unsaved changes...");
                if (Images.All(i => !i.IsEdited))
                {
                    _logService.Trace("There are no unsaved changes.  Exiting...");
                    return;
                }

                _logService.Trace("Prompting for confirmation...");
                e.Cancel = !_dialogService.Confirm("You have unsaved changes.  Do you wish to exit?",
                    "Unsaved Changes");
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

        private void Delete()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Deleting metadata for ""{_selectedImageViewModel.Filename}""...");
                _selectedImageViewModel?.Delete();
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

        public ICommand DeleteCommand => _deleteCommand ?? (_deleteCommand = new CommandHandler(Delete, DeleteEnabled));

        private bool DeleteEnabled()
        {
            _logService.TraceEnter();
            try
            {
                return _selectedImageViewModel != null && _selectedImageViewModel.HasMetadata;
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

        public ICommand ExitCommand => _exitCommand ?? (_exitCommand = new CommandHandler(Exit, true));

        public bool HasDateTaken => SelectedImageViewModel?.HasDateTaken ?? false;

        public bool HasRecentlyUsedDirectories => RecentlyUsedFolders.Count > 0;

        public bool HasStatus => _selectedImageViewModel != null;


        public ImageFormat ImageFormat
        {
            get => _selectedImageViewModel?.ImageFormat ?? _configurationService.ImageFormat;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace("Checking if default image format has changed...");
                    if (_configurationService.ImageFormat != value)
                    {
                        _logService.Trace("Default image format has changed.  Updating...");
                        _configurationService.ImageFormat = value;
                    }

                    _logService.Trace("Checking if there is a selected image...");
                    if (_selectedImageViewModel == null)
                    {
                        _logService.Trace("There is no selected image.  Returning...");
                        return;
                    }

                    _logService.Trace($@"Setting image format for ""{_selectedImageViewModel.Filename}""...");
                    _selectedImageViewModel.ImageFormat = value;

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

        public ObservableCollection<ImageViewModel> Images { get; }

        private void ImageViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var imageViewModel = (ImageViewModel) sender;

            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Changed property is ""{e.PropertyName}"".  Handling change...");
                switch (e.PropertyName)
                {
                    case "BackColorOpacity":
                        _logService.Trace("Checking if opacity is activated...");
                        if (imageViewModel.BackColorOpacity == "100%")
                        {
                            _logService.Trace("Opacity is not activated.  Exiting...");
                            break;
                        }

                        _logService.Trace($@"Creating color item for ""{imageViewModel.BackColor.ToString()}""...");
                        var colorItem = new ColorItem(imageViewModel.BackColor, imageViewModel.BackColor.ToString());

                        _logService.Trace($@"Checking if ""{imageViewModel.BackColor.ToString()}"" is already in list of recently used back colors...");
                        if (RecentlyUsedBackColors.Contains(colorItem))
                        {
                            _logService.Trace(
                                $@"""{imageViewModel.BackColor.ToString()}"" is already in list of recently used back colors.  Exiting...");
                            break;
                        }

                        // add the recently used colors
                        RecentlyUsedBackColors.Add(colorItem);

                        OnPropertyChanged(nameof(BackColorOpacity));

                        break;
                    case "Brightness":
                        (_resetBrightnessCommand as ICommandHandler)?.Notify();

                        break;
                    case "DateTaken":
                        OnPropertyChanged(nameof(QuickCaptions));

                        break;
                    case "HasDateTaken":
                        OnPropertyChanged(nameof(HasDateTaken));

                        break;
                    case "HasMetadata":
                        (_deleteCommand as ICommandHandler)?.Notify();

                        break;
                    case "Latitude":
                    case "Longitude":
                        (_whereCommand as ICommandHandler)?.Notify();

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

        private ObservableCollection<ColorItem> LoadRecentlyUsedBackColors()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Creating observable collection to return...");
                var observableCollection = new ObservableCollection<ColorItem>();

                _logService.Trace("Populating observable collection from configuration...");
                foreach (var color in _configurationService.RecentlyUsedBackColors)
                {
                    _logService.Trace($@"Adding ""{color}"" to list of recently used back colors...");
                    observableCollection.Add(new ColorItem(color, color.ToString()));
                }

                _logService.Trace("Watching for changes to observable collection...");
                observableCollection.CollectionChanged += RecentlyUsedBackColors_CollectionChanged;

                return observableCollection;
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

        public ICommand NextCommand => _nextCommand ?? (_nextCommand = new CommandHandler(Next, NextEnabled));


        private bool NextEnabled()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking the current position of the selected image...");
                if (_selectedImageViewModel == null)
                {
                    _logService.Trace("There is no selected image.  Returning...");
                    return false;
                }

                return _selectedIndex < Images.Count - 1;
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

        private void Open()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Prompting for directory...");
                var selectedPath = _dialogService.Browse("Where are your photos?", string.Empty);
                if (selectedPath == null)
                {
                    _logService.Trace("User has cancelled dialog.  Exiting...");
                    return;
                }

                _logService.Trace($@"Loading folder ""{selectedPath}""...");
                var folder = _folderService.Open(selectedPath);

                _logService.Trace($@"Checking if ""{selectedPath}"" has any subfolders...");
                if (folder.SubFolders.Any())
                {
                    _logService.Trace("Creating view model...");
                    var folderParameter = new ConstructorArgument("folder", folder);
                    var openFolderViewModel = NinjectKernel.Get<OpenFolderViewModel>(folderParameter);

                    _logService.Trace($@"Prompting for subfolders to include...");
                    if (_navigationService.ShowDialog<OpenFolderWindow>(openFolderViewModel) != true)
                    {
                        _logService.Trace("User cancelled dialog.  Exiting...");
                        return;
                    }

                    _logService.Trace("Updating subfolder selections...");
                    foreach (var subFolderViewModel in openFolderViewModel.SubFolders)
                    {
                        folder.SubFolders.First(f => f.Path == subFolderViewModel.Path).IsSelected =
                            subFolderViewModel.IsSelected;
                    }
                }

                _logService.Trace($@"Calling view model method with directory ""{selectedPath}""...");
                var folderViewModel = Mapper.Map<FolderViewModel>(folder);
                OpenDirectory(folderViewModel);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public ICommand OpenRecentlyUsedDirectoryCommand => _openRecentlyUsedDirectoryCommand ??
                                                            (_openRecentlyUsedDirectoryCommand =
                                                                new CommandHandler<FolderViewModel>(OpenDirectory, true));

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

        public IList<string> QuickCaptions => Images
            .Where(i => !string.IsNullOrWhiteSpace(i.Caption) && !string.IsNullOrWhiteSpace(i.DateTaken) &&
                        i.Caption != SelectedImageViewModel?.Caption &&
                        i.DateTaken == SelectedImageViewModel?.DateTaken).OrderBy(i => i.Caption)
            .Select(i => i.Caption.Replace("_", "__")).Distinct().ToList();

        public ObservableCollection<ColorItem> RecentlyUsedBackColors
        {
            get => _recentlyUsedBackColors;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace("Saving new list of recently used colours...");
                    _recentlyUsedBackColors = value;

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

        private void RecentlyUsedBackColors_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var recentlyUsedBackColors = (ObservableCollection<ColorItem>) sender;

            _logService.TraceEnter();
            try
            {
                _logService.Trace("Persisting recently used back colors...");
                _configurationService.RecentlyUsedBackColors =
                    recentlyUsedBackColors.Select(i => i.Color ?? Colors.Transparent).Take(11).ToList();
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

        public ObservableCollection<FolderViewModel> RecentlyUsedFolders { get; }

        public ImageViewModel SelectedImageViewModel
        {
            get => _selectedImageViewModel;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Setting value of {nameof(SelectedImageViewModel)}...");
                    _selectedImageViewModel = value;

                    if (value != null)
                    {
                        _logService.Trace($@"Loading ""{value.Filename}""...");
                        value.LoadImage(_openCancellationTokenSource.Token);
                        value.LoadPreview(false, _openCancellationTokenSource.Token);

                        _logService.Trace($@"Saving ""{value.Filename}"" as last selected image...");
                        _recentlyUsedDirectoriesService.SetLastSelectedFile(value.Filename);

                        // watch for changes to the date taken
                        value.PropertyChanged += ImageViewModel_PropertyChanged;
                    }

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BackColorOpacity));
                    OnPropertyChanged(nameof(HasDateTaken));
                    OnPropertyChanged(nameof(HasStatus));
                    OnPropertyChanged(nameof(ImageFormat));
                    OnPropertyChanged(nameof(QuickCaptions));

                    _logService.Trace("Checking which commands are enabled...");
                    CheckCommandsEnabled();
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

                    _logService.Trace("Checking if preview images can be removed...");
                    if ((_indexToRemove >= 0 && _indexToRemove < Images.Count - 1) && (_indexToRemove < value - 1 || _indexToRemove > value + 1))
                    {
                        _logService.Trace($"Removing image as position {_indexToRemove}...");
                        Images[_indexToRemove].UnloadImage();
                    }

                    _logService.Trace($"Updating index to remove...");
                    _indexToRemove = _nextIndexToRemove;
                    _nextIndexToRemove = value;

                    _logService.Trace(@"Checking if there is a next image...");
                    if (_selectedIndex < Images.Count - 1)
                    {
                        _logService.Trace($@"Preloading image at position {_selectedIndex + 1}...");
                        Images[_selectedIndex + 1].LoadOriginalImage();
                    }

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Status));
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        private void Settings()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Creating settings view model...");
                var settingsViewModel = NinjectKernel.Get<SettingsViewModel>();

                _logService.Trace("Showing settings window...");
                _navigationService.ShowDialog<SettingsWindow>(settingsViewModel);
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

        public ICommand SettingsCommand => _settingsCommand ?? (_settingsCommand = new CommandHandler(Settings, true));

        public string Status => $"{_selectedIndex + 1} of {Images.Count}";

        public string Title => Resources.ApplicationName;


        private void Where()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Displaying where photo was taken on UI...");
                _whereService.Open(_selectedImageViewModel?.Latitude ?? 0, _selectedImageViewModel?.Longitude ?? 0);
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

        public ICommand WhereCommand => _whereCommand ?? (_whereCommand = new CommandHandler(Where, WhereEnabled));


        private bool WhereEnabled()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if current image has geolocation...");
                return _selectedImageViewModel?.Latitude != null && _selectedImageViewModel.Longitude != null;
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

        private void ZoomIn()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Increasing size of caption font...");
                CaptionSize++;
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

        public ICommand ZoomInCommand => _zoomInCommand ?? (_zoomInCommand = new CommandHandler(ZoomIn, true));

        private void ZoomOut()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Checking if {nameof(CaptionSize)} can be reduced...");
                if (Math.Abs(CaptionSize - MinimumCaptionSize) <= double.Epsilon)
                {
                    _logService.Trace($"{nameof(CaptionSize)} cannot be reduced.  Exiting...");
                    return;
                }

                _logService.Trace($"Reducing value of {nameof(CaptionSize)}...");
                CaptionSize--;
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

        public ICommand ZoomOutCommand =>
            _zoomOutCommand ?? (_zoomOutCommand = new CommandHandler(ZoomOut, ZoomOutEnabled));

        private bool ZoomOutEnabled()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Current caption size is {CaptionSize}.  Checking if it can be reduced...");
                return Math.Abs(CaptionSize - MinimumCaptionSize) > double.Epsilon;
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


        public ICommand OpenCommand => _openCommand ?? (_openCommand = new CommandHandler(Open, true));

        private void OpenLastUsedDirectory()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Getting most recently used directory...");
                var mostRecentlyUsedFolder = _recentlyUsedDirectoriesService.GetMostRecentlyUsedDirectory();
                if (mostRecentlyUsedFolder == null)
                {
                    _logService.Trace("There is no most recently used directory.  Exiting...");
                    return;
                }

                _logService.Trace($@"Opening ""{mostRecentlyUsedFolder}"" on background thread...");
                var folderViewModel = Mapper.Map<FolderViewModel>(mostRecentlyUsedFolder);
                OpenDirectory(folderViewModel);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OpenDirectory(FolderViewModel folder)
        {
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                _logService.Trace("Creating view model for progress window...");
                var progressViewModel = NinjectKernel.Get<ProgressViewModel>();
                progressViewModel.Directory = folder.Path;

                _logService.Trace($@"Opening ""{folder.Path}"" on background thread...");
                _openCancellationTokenSource?.Cancel();
                _openCancellationTokenSource = new CancellationTokenSource();
                new Thread(OpenDirectoryThread).Start(new object[]
                    {folder, progressViewModel, _openCancellationTokenSource.Token});

                _logService.Trace("Showing progress window...");
                _navigationService.ShowDialog<ProgressWindow>(progressViewModel);
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private void OpenDirectoryThread(object state)
        {
            var stateArray = (object[]) state;
            var folderViewModel = (FolderViewModel) stateArray[0];
            var progressViewModel = (ProgressViewModel) stateArray[1];
            var cancellationToken = (CancellationToken) stateArray[2];

            // create dependencies
            var imageService = NinjectKernel.Get<IImageService>();
            var logService = NinjectKernel.Get<ILogService>();
            var recentlyUsedDirectoriesService = NinjectKernel.Get<IRecentlyUsedFoldersService>();

            var stopWatch = Stopwatch.StartNew();

            logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace("Clearing any in progress preview loads...");
                _taskScheduler.Clear();

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($@"Finding image files in ""{folderViewModel.Path}""...");
                var imageFilenames = (List<string>)imageService.Find(folderViewModel.Path);

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace("Searching subfolders for images...");
                imageFilenames.AddRange(folderViewModel.SubFolders.Where(sf => sf.IsSelected).SelectMany(sf => imageService.Find(Path.Combine(folderViewModel.Path, sf.Path))));

                // set the progress bar
                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($"Setting progress bar maximum to {imageFilenames.Count}");
                progressViewModel.Maximum = imageFilenames.Count;

                var images = new List<ImageViewModel>();

                // load the images backwards to queue the last image first
                for (var p = 0; p < imageFilenames.Count; p++)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace($"Updating progress for image at position {p}...");
                    progressViewModel.Value = p + 1;

                    if (cancellationToken.IsCancellationRequested) return;
                    var imageFilename = imageFilenames[p];
                    logService.Trace($@"Creating image view model for ""{imageFilename}""...");
                    images.Add(new ImageViewModel(imageFilename));
                }

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($"Adding {images.Count} images to list...");
                UpdateImages(images);

                logService.Trace($@"Adding ""{folderViewModel.Path}"" to recently used directories...");
                var folder = Mapper.Map<Folder>(folderViewModel);
                recentlyUsedDirectoriesService.Add(folder);

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($@"Checking if there are any images in ""{folderViewModel.Path}""...");
                if (Images.Count == 0)
                {
                    logService.Trace($@"No images were found in ""{folderViewModel.Path}"".  Exiting...");
                    return;
                }

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace("Getting most recently used image...");
                var mostRecentlyUsedImageViewModelFilename = _recentlyUsedDirectoriesService.GetMostRecentlyUsedFile();
                if (mostRecentlyUsedImageViewModelFilename == null)
                {
                    logService.Trace("No recently used image found.  Defaulting to start...");
                    SelectedImageViewModel = Images[0];
                }
                else
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace($@"Finding image view model ""{mostRecentlyUsedImageViewModelFilename}""...");
                    var selectedImageViewModel =
                        Images.FirstOrDefault(i => i.Filename == mostRecentlyUsedImageViewModelFilename);
                    if (selectedImageViewModel == null)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        logService.Trace(
                            $@"""{mostRecentlyUsedImageViewModelFilename}"" not found in directory.  Defaulting to first...");
                        SelectedImageViewModel = Images[0];
                    }
                    else
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        logService.Trace(
                            $@"Setting selected image to ""{mostRecentlyUsedImageViewModelFilename}"" on UI thread...");
                        Application.Current?.Dispatcher.Invoke(() => SelectedImageViewModel = selectedImageViewModel);
                    }
                }
            }
            finally
            {
                // hide the progress bar
                progressViewModel.Close = true;

                logService.TraceExit(stopWatch);
            }
        }

        private void ResetBrightness()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if there is a selected image...");
                if (_selectedImageViewModel == null)
                {
                    _logService.Trace("There is no selected image.  Exiting...");
                    return;
                }

                _logService.Trace($@"Resetting brightness of ""{_selectedImageViewModel.Filename}"" to 0...");
                _selectedImageViewModel.Brightness = 0;
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

        public ICommand ResetBrightnessCommand => _resetBrightnessCommand ??
                                                  (_resetBrightnessCommand = new CommandHandler(ResetBrightness,
                                                      ResetBrightnessEnabled));

        private bool ResetBrightnessEnabled()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if selected image has had brightness adjusted...");
                return (_selectedImageViewModel?.Brightness ?? 0) != 0;
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

        private bool RotateEnabled()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if there is a selected image...");
                return _selectedImageViewModel != null;
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

        private void RotateLeft()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Rotating selected image to the left...");
                _selectedImageViewModel?.RotateLeft();
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

        public ICommand RotateLeftCommand =>
            _rotateLeftCommand ?? (_rotateLeftCommand = new CommandHandler(RotateLeft, RotateEnabled));


        private void RotateRight()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Rotating selected image to the right...");
                _selectedImageViewModel?.RotateRight();
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

        public ICommand RotateRightCommand =>
            _rotateRightCommand ?? (_rotateRightCommand = new CommandHandler(RotateRight, RotateEnabled));

        private void Save()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if output path exists...");
                if (string.IsNullOrWhiteSpace(_configurationService.OutputPath) ||
                    !System.IO.Directory.Exists(_configurationService.OutputPath))
                {
                    _logService.Trace("Prompting user for output path...");
                    SaveAs();

                    return;
                }

                _logService.Trace($@"Saving to ""{_configurationService.OutputPath}""...");
                Save(_configurationService.OutputPath);
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

        private void Save(string outputPath)
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
                    return;
                }

                _logService.Trace($@"Getting output path for ""{_selectedImageViewModel.Filename}""...");
                var pathToSaveTo = _imageService.GetFilename(outputPath, _selectedImageViewModel.Filename,
                    _selectedImageViewModel.ImageFormat);

                _logService.Trace($@"Checking if ""{pathToSaveTo}"" already exists...");
                if (File.Exists(pathToSaveTo))
                {
                    _logService.Trace("Output file already exists.  Prompting to see if user wants to overwrite...");
                    if (!_dialogService.Confirm($@"""{pathToSaveTo}"" already exists.  Do you wish to overwrite it?",
                        "File Exists"))
                    {
                        _logService.Trace("User does not want to overwrite existing file.  Exiting...");
                        return;
                    }
                }

                _logService.Trace($@"Saving ""{_selectedImageViewModel.Filename}""...");
                _selectedImageViewModel.Save(outputPath);

                _logService.Trace("Checking if there is an image to move to...");
                if (_selectedIndex >= Images.Count - 1)
                {
                    _logService.Trace("There is no image to move to.  Exiting...");
                    return;
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
        }

        private void SaveAs()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Prompting for directory...");
                var outputPath = _dialogService.Browse("Where are your photos?", _configurationService.OutputPath);
                if (outputPath == null)
                {
                    _logService.Trace(("User cancelled folder selection..."));
                    return;
                }

                _logService.Trace($@"Saving selected image to ""{outputPath}""...");
                Save(outputPath);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public ICommand SaveAsCommand => _saveAsCommand ?? (_saveAsCommand = new CommandHandler(SaveAs, SaveEnabled));

        public ICommand SaveCommand => _saveCommand ?? (_saveCommand = new CommandHandler(Save, SaveEnabled));

        private bool SaveEnabled()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if there is a currently selected image...");
                return _selectedImageViewModel != null;
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

        private void UpdateImages(IList<ImageViewModel> images)
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
                        DispatcherPriority.Background, images);

                    return;
                }

                Images.Clear();
                foreach (var imageViewModel in images) Images.Add(imageViewModel);

                OnPropertyChanged(nameof(HasStatus));
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                logService.TraceExit();
            }
        }

        #region constants

        private const double MinimumCaptionSize = 8d;

        #endregion

        #region delegates

        private delegate void OnClearDelegate();
        private delegate void OnErrorDelegate(Exception error);
        private delegate void OnNextDelegate(Folder directory);
        private delegate void OnPropertyChangedDelegate(string propertyName);
        private delegate void UpdateImagesDelegate(IList<ImageViewModel> images);
        #endregion

        #region variables

        private readonly IDialogService _dialogService;
        private ICommand _closeCommand;
        private readonly IConfigurationService _configurationService;
        private ICommand _deleteCommand;
        private ICommand _exitCommand;
        private IFolderService _folderService;
        private readonly IImageService _imageService;
        private int _indexToRemove;
        private readonly ILogService _logService;
        private readonly INavigationService _navigationService;
        private ICommand _nextCommand;
        private int _nextIndexToRemove;
        private readonly IList<IObserver> _observers;
        private ICommand _openRecentlyUsedDirectoryCommand;
        private string _outputPath;
        private readonly IRecentlyUsedFoldersService _recentlyUsedDirectoriesService;
        private ImageViewModel _selectedImageViewModel;
        private int _selectedIndex;
        private IOpacityService _opacityService;
        private CancellationTokenSource _openCancellationTokenSource;
        private ICommand _openCommand;
        private readonly SingleTaskScheduler _taskScheduler;
        private bool _disposedValue; // To detect redundant calls
        private ObservableCollection<ColorItem> _recentlyUsedBackColors;
        private readonly CancellationTokenSource _recentlyUsedDirectoriesCancellationTokenSource;
        private ICommand _resetBrightnessCommand;
        private ICommand _rotateLeftCommand;
        private ICommand _rotateRightCommand;
        private ICommand _saveCommand;
        private ICommand _saveAsCommand;
        private ICommand _settingsCommand;
        private ICommand _whereCommand;
        private readonly IWhereService _whereService;
        private ICommand _zoomInCommand;
        private ICommand _zoomOutCommand;

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
                RecentlyUsedFolders.Clear();
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

        public void OnNext(Folder directory)
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
                var viewModel = Mapper.Map<FolderViewModel>(directory);

                logService.Trace($@"Adding ""{directory.Path}"" to recently used directories...");
                RecentlyUsedFolders.Add(viewModel);

                OnPropertyChanged(nameof(HasRecentlyUsedDirectories));
            }
            finally
            {
                logService.TraceExit();
            }
        }

        #endregion
    }
}