using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoMapper;
using PhotoLabel.Properties;
using PhotoLabel.Services;

namespace PhotoLabel
{
    public class FormMainViewModel : IDisposable, INotifyPropertyChanged, IDirectoryOpenerObserver,
        IQuickCaptionObserver, IRecentlyUsedDirectoriesObserver
    {
        #region delegates
        public delegate void ImageFoundEventHandler(object sender, ImageFoundEventArgs e);
        public delegate void OpeningEventHandler(object sender, OpeningEventArgs e);
        public delegate void QuickCaptionHandler(object sender, QuickCaptionEventArgs e);
        public delegate void RecentlyUsedDirectoryHandler(object sender, RecentlyUsedDirectoryEventArgs e);

        private delegate void OnDelegate();
        private delegate void OnQuickCaptionDelegate(string caption);
        private delegate void OnRecentlyUsedDirectoryDelegate(Models.Directory recentlyUsedDirectory);
        #endregion

        #region constants

        private const double Tolerance = double.Epsilon;

        #endregion

        #region events
        public event ImageFoundEventHandler ImageFound;
        public event QuickCaptionHandler QuickCaption;
        public event EventHandler QuickCaptionCleared;
        public event EventHandler RecentlyUsedDirectoriesCleared;
        public event RecentlyUsedDirectoryHandler RecentlyUsedDirectory;
        #endregion

        #region variables
        private readonly IConfigurationService _configurationService;
        private readonly IDirectoryOpenerService _directoryOpenerService;
        private readonly object _imageLock = new object();
        private readonly ManualResetEvent _imageManualResetEvent;
        private readonly IImageMetadataService _imageMetadataService;
        private readonly IList<Models.ImageModel> _images = new List<Models.ImageModel>();
        private readonly IImageService _imageService;
        private readonly object _imagesLock = new object();
        private readonly ILogService _logService;
        private readonly IQuickCaptionService _quickCaptionService;
        private readonly IRecentlyUsedDirectoriesService _recentlyUsedDirectoriesService;
        private Models.ImageModel _current;
        private bool _disposed;
        private Image _image;
        private CancellationTokenSource _imageCancellationTokenSource;
        private CancellationTokenSource _openCancellationTokenSource;
        private int _position = -1;
        private CancellationTokenSource _recentlyUsedDirectoriesCancellationTokenSource;
        #endregion

        public FormMainViewModel(
            IConfigurationService configurationService,
            IDirectoryOpenerService directoryOpenerService,
            IImageMetadataService imageMetadataService,
            IImageService imageService,
            ILogService logService,
            IQuickCaptionService quickCaptionService,
            IRecentlyUsedDirectoriesService recentlyUsedDirectoriesService)
        {
            // save dependency injections
            _configurationService = configurationService;
            _directoryOpenerService = directoryOpenerService;
            _imageMetadataService = imageMetadataService;
            _imageService = imageService;
            _logService = logService;
            _quickCaptionService = quickCaptionService;
            _recentlyUsedDirectoriesService = recentlyUsedDirectoriesService;

            // initialise variables
            _disposed = false;
            _image = new Bitmap(1, 1);
            _imageManualResetEvent = new ManualResetEvent(true);

            // load the second colour images
            if (_configurationService.BackgroundSecondColour != null)
                BackgroundSecondColourImage =
                    _imageService.Circle(_configurationService.BackgroundSecondColour.Value, 16, 16);

            if (_configurationService.SecondColour != null)
                SecondColourImage = _imageService.Circle(_configurationService.SecondColour.Value, 16, 16);

            // subscribe to events from the services
            _directoryOpenerService.Subscribe(this);
            _quickCaptionService.Subscribe(this);
            _recentlyUsedDirectoriesService.Subscribe(this);
        }

        public bool AppendDateTakenToCaption
        {
            get => _current?.AppendDateTakenToCaption ?? _configurationService.AppendDateTakenToCaption;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(AppendDateTakenToCaption)} has changed...");
                    if (_configurationService.AppendDateTakenToCaption == value)
                    {
                        _logService.Trace($"Value of {nameof(AppendDateTakenToCaption)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(AppendDateTakenToCaption)}...");
                    _configurationService.AppendDateTakenToCaption = value;

                    // save the colour to the image
                    if (_current != null)
                    {
                        // save the value on the image
                        _current.AppendDateTakenToCaption = value;

                        // redraw the image on a background thread
                        LoadImage(_position);
                    }

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public Color BackgroundColour
        {
            get => _current?.BackgroundColour ?? _configurationService.BackgroundColour;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($@"Checking if value of {nameof(BackgroundColour)} has changed...");
                    if (BackgroundColour == value)
                    {
                        _logService.Trace($@"Value of {nameof(BackgroundColour)} has not changed.  Exiting...");
                        return;
                    }

                    // save the current colour as the secondary background colour
                    BackgroundSecondColour = BackgroundColour;

                    if (_current != null)
                    {
                        _logService.Trace($@"Setting new value of {nameof(BackgroundColour)} for existing image...");
                        _current.BackgroundColour = value;

                        // redraw the image
                        LoadImage(_position);
                    }

                    // save the current background colour as the secondary background colour
                    // save this as the default background color
                    _configurationService.BackgroundColour = value;

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public Color? BackgroundSecondColour
        {
            get => _configurationService.BackgroundSecondColour;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($@"Checking if value of {nameof(BackgroundSecondColour)} has changed...");
                    if (BackgroundSecondColour == value)
                    {
                        _logService.Trace($@"Value of {nameof(BackgroundSecondColour)} has not changed.  Exiting...");
                        return;
                    }

                    // create the new background colour image
                    if (value == null)
                        BackgroundSecondColourImage = null;
                    else if (value.Value.A == 0)
                        BackgroundSecondColourImage = null;
                    else
                        BackgroundSecondColourImage = _imageService.Circle(value.Value, 16, 16);

                    // save the new value
                    _configurationService.BackgroundSecondColour = value;
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public Image BackgroundSecondColourImage { get; private set; }

        public bool CanDelete => _current?.IsMetadataLoaded ?? false;

        public string Caption
        {
            get => _current?.Caption ?? string.Empty;
            set
            {
                // only process changes
                if (Caption == value) return;

                // do we have an existing image?
                if (_current == null) return;

                // save the new value
                _current.Caption = value;

                // redraw the image
                LoadImage(_position);

                OnPropertyChanged();
            }
        }

        public CaptionAlignments CaptionAlignment
        {
            get => _current?.CaptionAlignment ?? _configurationService.CaptionAlignment;
            set
            {
                // only process changes
                if (CaptionAlignment == value) return;

                // do we have an existing image?
                if (_current != null)
                {
                    // update the value on the image
                    _current.CaptionAlignment = value;

                    // redraw the image
                    LoadImage(_position);
                }

                // save this as the default caption alignment
                _configurationService.CaptionAlignment = value;

                OnPropertyChanged();
            }
        }

        public Color Colour
        {
            get => _current?.Colour ?? _configurationService.Colour;
            set
            {
                // only process changes
                if (Colour.Equals(value)) return;
                //if (Colour.ToArgb() == value.ToArgb()) return;

                // save the current colour as the secondary colour
                SecondColour = Colour;

                // save the colour to the image
                if (_current != null)
                {
                    // save the colour on the image
                    _current.Colour = value;

                    // redraw the image on a background thread
                    LoadImage(_position);
                }

                // save the new default colour
                _configurationService.Colour = value;

                OnPropertyChanged();
            }
        }

        public int Count => _images.Count;

        public string DateTaken => _current?.DateTaken;

        public string Filename => _current?.Filename;

        public bool FontBold
        {
            get => _current?.FontBold ?? _configurationService.FontBold;
            set
            {
                // only process changes
                if (FontBold == value) return;

                // save as the default
                _configurationService.FontBold = value;

                // is there a current image?
                if (_current == null) return;

                // update the font size on the image
                _current.FontBold = value;

                // reload the im
                LoadImage(_position);

                OnPropertyChanged();
            }
        }

        public string FontName
        {
            get => _current?.FontName ?? _configurationService.FontName;
            set
            {
                // only process changes
                if (_configurationService.FontName == value) return;

                // save the new value
                _configurationService.FontName = value;

                // is there a current image?
                if (_current == null) return;

                // update the value on the current image
                _current.FontName = value;

                // update the image
                LoadImage(_position);

                OnPropertyChanged();
            }
        }

        public float FontSize
        {
            get => _current?.FontSize ?? _configurationService.FontSize;
            set
            {
                _logService.Trace("Checking if value is greater than 0...");
                if (value <= 0f) throw new ArgumentOutOfRangeException();

                // only process changes
                if (Math.Abs(FontSize - value) < Tolerance) return;

                // save the default value
                _configurationService.FontSize = value;

                // is there a current image?
                if (_current == null) return;

                // update the font size on the image
                _current.FontSize = value;

                // reload the image
                LoadImage(_position);

                OnPropertyChanged();
            }
        }

        public string FontType
        {
            get => _current?.FontType ?? _configurationService.FontType;
            set
            {
                // only process changes
                if (FontType == value) return;

                // validate that it is one of the available options
                if (value != "%" && value != "pts") throw new ArgumentOutOfRangeException(nameof(FontType));

                // save the new value
                _configurationService.FontType = value;

                // is there a current image?
                if (_current == null) return;

                // update the type on the image
                _current.FontType = value;

                // reload the image
                LoadImage(_position);

                OnPropertyChanged();
            }
        }

        public Image Image
        {
            get => _image;
            private set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if {nameof(Image)} value has changed...");
                    if (_image == value)
                    {
                        _logService.Trace($"Value of {nameof(Image)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Seeting new value of {nameof(Image)}...");
                    _image = value;

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public ImageFormat ImageFormat
        {
            get => _current?.ImageFormat ?? _configurationService.ImageFormat;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(ImageFormat)} has changed...");
                    if (Equals(_configurationService.ImageFormat, value))
                    {
                        _logService.Trace($"Value of {nameof(ImageFormat)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($@"Setting value of {nameof(ImageFormat)} to {value}...");
                    _configurationService.ImageFormat = value;

                    _logService.Trace("Checking if there is a current image...");
                    if (_current != null)
                    {
                        _logService.Trace("There is a current image.  Updating image format...");
                        _current.ImageFormat = value;
                    }

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public IInvoker Invoker { get; set; }

        public float? Latitude => _current?.Latitude;

        public float? Longitude => _current?.Longitude;

        public string OutputFilename => _current == null
            ? string.Empty
            : Path.Combine(OutputPath,
                $"{Path.GetFileName(_current.Filename)}.{(_current.ImageFormat == ImageFormat.Jpeg ? "jpg" : _current.ImageFormat == ImageFormat.Bmp ? "bmp" : "png")}");

        public string OutputPath
        {
            get => _configurationService.OutputPath;
            set
            {
                _configurationService.OutputPath = value;

                OnPropertyChanged();
            }
        }

        public int Position
        {
            get => _position;
            set
            {
                // only process changes
                if (_position == value) return;

                // save the change
                _position = value;

                // set the current image
                _current = _images[value];

                // save the last selected filename for this folder
                _recentlyUsedDirectoriesService.SetLastSelectedFile(_current.Filename);

                // redraw the image on a background thread
                LoadImage(value);

                // cache the next image on a background thread
                CacheImage(value + 1);

                OnPropertyChanged();
            }
        }

        public Rotations Rotation
        {
            get => _current?.Rotation ?? Rotations.Zero;
            set
            {
                // only process changes
                if (Rotation == value) return;

                // save the rotation to the image
                if (_current == null) return;

                // update the value
                _current.Rotation = value;

                // redraw the image
                LoadImage(_position);

                OnPropertyChanged();
            }
        }

        public Color? SecondColour
        {
            get => _configurationService.SecondColour;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($@"Checking if value of {nameof(SecondColour)} has changed...");
                    if (SecondColour?.ToArgb() == value?.ToArgb())
                    {
                        _logService.Trace($@"Value of {nameof(SecondColour)} has not changed.  Exiting...");
                        return;
                    }

                    // create the new background colour image
                    if (value == null)
                        SecondColourImage = null;
                    else
                        SecondColourImage = _imageService.Circle(value.Value, 16, 16);

                    // save the new value
                    _configurationService.SecondColour = value;
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public Image SecondColourImage { get; private set; }

        public FormWindowState WindowState
        {
            get => _configurationService.WindowState;
            set
            {
                // ignore when the form is minimised
                if (value == FormWindowState.Minimized) return;

                // only process changes
                if (_configurationService.WindowState == value) return;

                // save the new value
                _configurationService.WindowState = value;

                OnPropertyChanged();
            }
        }

        void IDirectoryOpenerObserver.OnOpening(string directory)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Resetting state...");
                _images.Clear();
                _quickCaptionService.Clear();
                _current = null;
                Image = null;
                Position = -1;

                _logService.Trace("Letting UI know that a directory is being opened...");
                OnOpening(directory);

                _logService.Trace("Resetting property values...");
                OnPropertyChanged(nameof(Caption));
                OnPropertyChanged(nameof(CaptionAlignment));
                OnPropertyChanged(nameof(Colour));
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(nameof(DateTaken));
                OnPropertyChanged(nameof(Filename));
                OnPropertyChanged(nameof(FontBold));
                OnPropertyChanged(nameof(FontName));
                OnPropertyChanged(nameof(FontSize));
                OnPropertyChanged(nameof(FontType));
                OnPropertyChanged(nameof(ImageFormat));
                OnPropertyChanged(nameof(Latitude));
                OnPropertyChanged(nameof(Rotation));
                OnPropertyChanged(nameof(Longitude));
                OnPropertyChanged(nameof(OutputFilename));
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        void IDirectoryOpenerObserver.OnOpened(string directory, int count)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Notifying UI that directory has been opened...");
                OnOpened(directory, count);

                _logService.Trace("Checking if any images were found...");
                if (count == 0)
                {
                    _logService.Trace("No images were found.  Exiting...");
                    return;
                }

                _logService.Trace(
                    $"{count} images were found.  Getting most recently used file for most recently used directory...");
                var mostRecentlyUsedFile = _recentlyUsedDirectoriesService.GetMostRecentlyUsedFile();

                _logService.Trace("Checking if there is a most recently used file...");
                if (string.IsNullOrWhiteSpace(mostRecentlyUsedFile))
                {
                    _logService.Trace("There is no most recently used file.  Defaulting to first image...");
                    Position = 0;

                    return;
                }

                _logService.Trace($@"Finding position of ""{mostRecentlyUsedFile}""...");
                var imageModel = _images.FirstOrDefault(i => i.Filename == mostRecentlyUsedFile);

                _logService.Trace($@"Checking if position of ""{mostRecentlyUsedFile}"" could be found...");
                if (imageModel == null)
                {
                    _logService.Trace($@"Position of ""{mostRecentlyUsedFile}"" could not be found.  Exiting...");
                    return;
                }

                var position = _images.IndexOf(imageModel);

                _logService.Trace($"Setting position to {position}...");
                Position = position;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void OnError(Exception ex)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (Invoker?.InvokeRequired ?? false)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoker.Invoke(new OnErrorDelegate(OnError), ex);

                    return;
                }

                _logService.Trace("Notifying error...");
                Error?.Invoke(this, new ErrorEventArgs(ex));
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void OnImageFound(string directory, Services.Models.Metadata file)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Adding ""{directory}"" to recently used directories list...");
                _recentlyUsedDirectoriesService.Add(directory);

                _logService.Trace("Creating image model...");
                var imageModel = Mapper.Map<Models.ImageModel>(file);

                _logService.Trace($@"Adding ""{file.Filename}"" to image list...");
                _images.Add(imageModel);

                _logService.Trace("Mapping to service layer...");
                var metadata = Mapper.Map<Services.Models.Metadata>(imageModel);

                _logService.Trace($@"Adding ""{metadata.Caption}"" to quick captions...");
                _quickCaptionService.Add(metadata);

                OnImageFound(file.Filename);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void Dispose()
        {
            // dispose of managed resources
            Dispose(true);

            // suppress finalisation
            GC.SuppressFinalize(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void IQuickCaptionObserver.OnClear()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Bubbling up to UI...");
                OnQuickCaptionCleared();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        void IQuickCaptionObserver.OnCompleted()
        {
            // nothing required
        }

        public void OnNext(string caption)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Adding ""{caption}"" to quick captions...");
                OnQuickCaption(caption);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        void IRecentlyUsedDirectoriesObserver.OnClear()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Notifying UI that recently used directories have been cleared...");
                OnRecentlyUsedDirectoriesCleared();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void OnNext(Services.Models.Directory directory)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Mapping to UI layer...");
                var recentlyUsedDirectory = Mapper.Map<Models.Directory>(directory);

                _logService.Trace($@"Adding {recentlyUsedDirectory} to list of recently used directories...");
                OnRecentlyUsedDirectory(recentlyUsedDirectory);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private delegate void OnErrorDelegate(Exception ex);

        private delegate void OnOpeningDelegate(string directory);

        private delegate void OnPreviewLoadedDelegate(string filename, Image image);

        private delegate void OnPropertyChangedDelegate(string propertyName);

        private delegate void OnImageFoundDelegate(string filename);

        private void CacheImage(int position)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Position to cache is {position}");
                if (position > _images.Count - 2)
                {
                    _logService.Trace("There is no image to cache.  Exiting...");
                    return;
                }

                // get the name of the file to be cached
                var filename = _images[position].Filename;

                _logService.Trace($"Caching \"{filename}\" on background thread...");
                Task.Factory.StartNew(() => _imageService.Get(filename), _openCancellationTokenSource.Token,
                        TaskCreationOptions.LongRunning, TaskScheduler.Current)
                    .ContinueWith(OnError, _openCancellationTokenSource.Token, TaskContinuationOptions.OnlyOnFaulted);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public bool Delete()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Is there a current image?");
                if (_current == null)
                {
                    _logService.Trace("There is no current image.  Returning...");
                    return false;
                }

                _logService.Trace($"Deleting metadata for \"{_current.Filename}\"...");
                if (!_imageMetadataService.Delete(_current.Filename)) return false;

                _logService.Trace("Resetting image...");
                _current.IsExifLoaded = false;
                _current.IsMetadataLoaded = false;
                _current.Rotation = Rotations.Zero;
                _current.IsSaved = false;

                _logService.Trace("Checking that the output file can be deleted...");
                if (_current.OutputFilename != null &&
                    _current.OutputFilename != _current.Filename &&
                    File.Exists(_current.OutputFilename))
                {
                    _logService.Trace($"Deleting \"{_current.OutputFilename}\"...");
                    File.Delete(_current.OutputFilename);
                }

                _logService.Trace("Reloading image...");
                LoadImage(_position);

                _logService.Trace("Reloading preview...");
                LoadPreview(_current.Filename, _openCancellationTokenSource.Token);

                return true;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            // only dispose of managed resources once
            if (_disposed) return;

            if (disposing)
            {
                // cancel any active background jobs
                _openCancellationTokenSource?.Cancel();
                _recentlyUsedDirectoriesCancellationTokenSource?.Cancel();

                // dispose of any managed resources
                _openCancellationTokenSource?.Dispose();
                _recentlyUsedDirectoriesCancellationTokenSource?.Dispose();
            }

            _disposed = true;
        }

        public event ErrorEventHandler Error;

        private void ExifThread(Models.ImageModel imageModel, EventWaitHandle manualResetEvent)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Checking if Exif data is already loaded for \"{imageModel.Filename}\"...");
                if (!imageModel.IsExifLoaded)
                {
                    _logService.Trace($"Loading Exif data for \"{imageModel.Filename}\"...");
                    var exifData = _imageService.GetExifData(imageModel.Filename);
                    if (exifData != null)
                    {
                        _logService.Trace($"Populating values from Exif data for \"{imageModel.Filename}\"...");
                        _logService.Trace($"Date taken for \"{imageModel.Filename}\" is \"{exifData.DateTaken}\"");
                        imageModel.Caption = exifData.Title;
                        imageModel.DateTaken = exifData.DateTaken;
                        imageModel.Latitude = exifData.Latitude;
                        imageModel.Longitude = exifData.Longitude;
                    }

                    // flag that the Exif data is loaded
                    imageModel.IsExifLoaded = true;
                }
                else
                {
                    _logService.Trace(
                        $"Exif data is already loaded for \"{imageModel.Filename}\".  Caption is \"{imageModel.Caption}\"");
                }

                // flag that the Exif is loaded
                manualResetEvent.Set();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ImageThread(Models.ImageModel imageModel, CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;

                // load the image on another thread
                if (cancellationToken.IsCancellationRequested) return;
                var task = Task<Image>.Factory.StartNew(() => _imageService.Get(imageModel.Filename), cancellationToken,
                    TaskCreationOptions.LongRunning, TaskScheduler.Current);
                task.ContinueWith(OnError, cancellationToken, TaskContinuationOptions.OnlyOnFaulted);

                // if there was no metadata file, we need to load the Exif 
                // data to get the default caption
                if (cancellationToken.IsCancellationRequested) return;
                if (!imageModel.IsMetadataLoaded && !imageModel.IsExifLoaded)
                {
                    var exifResetEvent = new ManualResetEvent(false);

                    Task.Factory.StartNew(() => ExifThread(imageModel, exifResetEvent), cancellationToken,
                            TaskCreationOptions.LongRunning, TaskScheduler.Current)
                        .ContinueWith(OnError, cancellationToken, TaskContinuationOptions.OnlyOnFaulted);

                    // wait for the Exif data to load
                    exifResetEvent.WaitOne();
                }

                // get the image
                // this will wait until the thread has completed
                var image = task.Result;

                // work out the values to use
                var backgroundColour = imageModel.BackgroundColour ?? _configurationService.BackgroundColour;
                var captionAlignment = imageModel.CaptionAlignment ?? _configurationService.CaptionAlignment;
                var colour = imageModel.Colour ?? _configurationService.Colour;
                var fontBold = imageModel.FontBold ?? _configurationService.FontBold;
                var fontName = imageModel.FontName ?? _configurationService.FontName;
                var fontSize = imageModel.FontSize ?? _configurationService.FontSize;
                var fontType = imageModel.FontType ?? _configurationService.FontType;
                var rotation = imageModel.Rotation ?? Rotations.Zero;

                // what is the caption?
                var captionBuilder = new StringBuilder(imageModel.Caption);

                // is there a date taken?
                _logService.Trace($@"Checking if ""{imageModel.Filename}"" has a date taken set...");
                if (imageModel.DateTaken != null &&
                    (imageModel.AppendDateTakenToCaption ?? _configurationService.AppendDateTakenToCaption))
                {
                    if (captionBuilder.Length > 0) captionBuilder.Append(" - ");

                    captionBuilder.Append(imageModel.DateTaken);
                }

                var caption = captionBuilder.ToString();

                // create the caption
                if (cancellationToken.IsCancellationRequested) return;
                _logService.Trace($@"Caption for ""{imageModel.Filename}"" is ""{caption}"".  Creating image...");
                var captionedImage = _imageService.Caption(image, caption, captionAlignment, fontName, fontSize,
                    fontType, fontBold, new SolidBrush(colour), backgroundColour, rotation);
                try
                {
                    // update the image in a thread safe manner
                    if (cancellationToken.IsCancellationRequested) return;
                    lock (_imageLock)
                    {
                        if (cancellationToken.IsCancellationRequested) return;

                        _logService.Trace($@"Setting ""{imageModel.Filename}"" as current image...");
                        _current = imageModel;

                        _logService.Trace($@"Setting image for ""{imageModel.Filename}""...");
                        Image = captionedImage;
                    }
                }
                finally
                {
                    if (cancellationToken.IsCancellationRequested) captionedImage.Dispose();
                }

                _logService.Trace("Mapping to service layer...");
                var metadata = Mapper.Map<Services.Models.Metadata>(imageModel);

                _logService.Trace($@"Loading new list of quick captions for ""{metadata.Filename}""...");
                _quickCaptionService.Switch(metadata);

                // flag that the image has loaded
                _imageManualResetEvent.Set();

                if (cancellationToken.IsCancellationRequested) return;
                OnPropertyChanged(nameof(Image));
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void LoadImage(int position)
        {
            _logService.TraceEnter();
            try
            {
                lock (_imageLock)
                {
                    // cancel any in progress load
                    _imageCancellationTokenSource?.Cancel();

                    _logService.Trace("Creating new cancellation token...");
                    _imageCancellationTokenSource = new CancellationTokenSource();

                    // flag that the image is loading
                    _imageManualResetEvent.Reset();

                    // clear the image
                    Image = null;

                    // get the image to load
                    var imageToLoad = _images[position];

                    // load the image on a background thread
                    Task.Delay(300, _imageCancellationTokenSource.Token)
                        .ContinueWith((t, o) => ImageThread(imageToLoad, _imageCancellationTokenSource.Token), null,
                            _imageCancellationTokenSource.Token, TaskContinuationOptions.LongRunning,
                            TaskScheduler.Current)
                        .ContinueWith(OnError, _imageCancellationTokenSource.Token,
                            TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void LoadPreview(string filename, CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                // find the image being previewed
                _logService.Trace($@"Finding image ""{filename}""...");
                var imageModel = _images.FirstOrDefault(i => i.Filename == filename);
                if (imageModel == null)
                {
                    _logService.Trace($@"Image ""{filename}"" does not exist.  Exiting...");
                    return;
                }

                _logService.Trace($@"Loading preview for ""{filename}"" on a background thread...");
                Task.Factory.StartNew(() => PreviewThread(imageModel, cancellationToken),
                        _openCancellationTokenSource.Token)
                    .ContinueWith(OnError, cancellationToken, TaskContinuationOptions.OnlyOnFaulted);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void LoadRecentlyUsedDirectories()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Cancelling any in progress load...");
                _recentlyUsedDirectoriesCancellationTokenSource?.Cancel();

                _logService.Trace("Creating new cancellation token...");
                _recentlyUsedDirectoriesCancellationTokenSource = new CancellationTokenSource();

                // load the recently used directories on a background thread
                Task.Factory
                    .StartNew(
                        () => RecentlyUsedDirectoriesThread(_recentlyUsedDirectoriesCancellationTokenSource.Token),
                        _recentlyUsedDirectoriesCancellationTokenSource.Token, TaskCreationOptions.LongRunning,
                        TaskScheduler.Current)
                    .ContinueWith(OnError, null, TaskContinuationOptions.OnlyOnFaulted);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OnError(Task task, object state)
        {
            _logService.TraceEnter();
            try
            {
                OnError(task.Exception);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OnImageFound(string filename)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (Invoker?.InvokeRequired ?? false)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoker.Invoke(new OnImageFoundDelegate(OnImageFound), filename);

                    return;
                }

                ImageFound?.Invoke(this, new ImageFoundEventArgs {Filename = filename});
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OnOpened(string directory, int count)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (Invoker?.InvokeRequired ?? false)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoker.Invoke(new OnOpenedDelegate(OnOpened), directory, count);

                    return;
                }

                Opened?.Invoke(this, new OpenedEventArgs
                {
                    Directory = directory,
                    Count = count
                });
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OnOpening(string directory)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (Invoker?.InvokeRequired ?? false)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoker.Invoke(new OnOpeningDelegate(OnOpening), directory);

                    return;
                }

                _logService.Trace($@"Notifying ""{directory} opening...");
                Opening?.Invoke(this, new OpeningEventArgs {Directory = directory});
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OnPreviewLoaded(string filename, Image image)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (Invoker?.InvokeRequired ?? false)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoker.Invoke(new OnPreviewLoadedDelegate(OnPreviewLoaded), filename, image);

                    return;
                }

                _logService.Trace($@"Notifying preview loaded for ""{filename}""...");
                PreviewLoaded?.Invoke(this, new PreviewLoadedEventArgs {Filename = filename, Image = image});
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (Invoker?.InvokeRequired ?? false)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoker.Invoke(new OnPropertyChangedDelegate(OnPropertyChanged), propertyName);

                    return;
                }

                _logService.Trace($"Notifying change to {propertyName}...");
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OnQuickCaption(string caption)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (Invoker?.InvokeRequired ?? false)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoker.Invoke(new OnQuickCaptionDelegate(OnQuickCaption), caption);

                    return;
                }

                QuickCaption?.Invoke(this, new QuickCaptionEventArgs{Caption=caption});
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OnQuickCaptionCleared()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (Invoker?.InvokeRequired ?? false)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoker.Invoke(new OnDelegate(OnQuickCaptionCleared));

                    return;
                }

                QuickCaptionCleared?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OnRecentlyUsedDirectoriesCleared()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (Invoker?.InvokeRequired ?? false)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoker.Invoke(new OnDelegate(OnRecentlyUsedDirectoriesCleared));

                    return;
                }

                RecentlyUsedDirectoriesCleared?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OnRecentlyUsedDirectory(Models.Directory recentlyUsedDirectory)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (Invoker?.InvokeRequired ?? false)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoker.Invoke(new OnRecentlyUsedDirectoryDelegate(OnRecentlyUsedDirectory), recentlyUsedDirectory);

                    return;
                }

                RecentlyUsedDirectory?.Invoke(this, new RecentlyUsedDirectoryEventArgs { RecentlyUsedDirectory=recentlyUsedDirectory });
            }
            finally
            {
                _logService.TraceExit();
            }
        }
        public void Open(string directory)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));

            _logService.TraceEnter();
            try
            {
                _logService.Trace("Cancelling any in progress open...");
                _openCancellationTokenSource?.Cancel();

                _logService.Trace($@"Opening ""{directory}"" on a background thread...");
                _openCancellationTokenSource = new CancellationTokenSource();

                Task.Factory.StartNew(() => OpenThread(directory, _openCancellationTokenSource.Token), _openCancellationTokenSource.Token,
                        TaskCreationOptions.LongRunning, TaskScheduler.Current)
                    .ContinueWith(OnError, _openCancellationTokenSource.Token,
                        TaskContinuationOptions.OnlyOnFaulted);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        #region events

        public event OpenedEventHandler Opened;

        #endregion

        public event OpeningEventHandler Opening;

        private void OpenThread(string directory, CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                lock (_imagesLock)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    _logService.Trace("Clearing current images...");
                    _current = null;
                    _images.Clear();
                    _position = -1;
                    lock (_imageLock) Image = null;

                    if (cancellationToken.IsCancellationRequested) return;
                    _logService.Trace($@"Retrieving image filenames from ""{directory}"" and it's sub-folders...");
                    _directoryOpenerService.Find(directory, cancellationToken);

                    if (cancellationToken.IsCancellationRequested) return;
                    _logService.Trace($"Loading previews for {_images.Count} images...");
                    foreach (var imageModel in _images)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        PreviewThread(imageModel, cancellationToken);
                    }
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public event PreviewLoadedEventHandler PreviewLoaded;

        private void PreviewThread(Models.ImageModel image, CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                lock (image)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    _logService.Trace($@"Checking if preview is already loaded for ""{image.Filename}""...");
                    if (image.IsPreviewLoaded)
                    {
                        _logService.Trace($@"Preview is already loaded for ""{image.Filename}"".  Exiting...");
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested) return;
                    var preview = _imageService.Get(image.Filename, 128, 128);

                    if (cancellationToken.IsCancellationRequested) return;
                    if (image.IsSaved)
                        preview = _imageService.Overlay(preview, Resources.saved,
                            preview.Width - Resources.saved.Width - 4, 4);
                    else if (image.IsMetadataLoaded)
                        preview = _imageService.Overlay(preview, Resources.metadata,
                            preview.Width - Resources.saved.Width - 4, 4);

                    _logService.Trace($@"Flagging that preview has been loaded for ""{image.Filename}""...");
                    image.IsPreviewLoaded = true;

                    if (cancellationToken.IsCancellationRequested) return;
                    OnPreviewLoaded(image.Filename, preview);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void RecentlyUsedDirectoriesThread(CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Loading recently used directories...");
                _recentlyUsedDirectoriesService.Load(cancellationToken);

                _logService.Trace("Getting most recently used directory...");
                var mostRecentlyUsedDirectory = _recentlyUsedDirectoriesService.GetMostRecentlyUsedDirectory();

                _logService.Trace("Checking if there is a most recently used directory...");
                if (string.IsNullOrWhiteSpace(mostRecentlyUsedDirectory))
                {
                    _logService.Trace("No most recently used directory found.  Exiting...");
                    return;
                }

                _logService.Trace($@"Opening ""{mostRecentlyUsedDirectory}""...");
                Open(mostRecentlyUsedDirectory);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void Save(string filename)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            if (_current == null) throw new InvalidOperationException("There is no current image");

            _logService.TraceEnter();
            try
            {
                // wait for the image to load
                _imageManualResetEvent.WaitOne();

                // save the image
                _logService.Trace("Saving image to disk...");
                lock (Image)
                {
                    _imageService.Save(Image, filename, ImageFormat);
                }

                // save the metadata
                var metadata = new Services.Models.Metadata
                {
                    BackgroundColour = BackgroundColour.ToArgb(),
                    Caption = Caption,
                    CaptionAlignment = CaptionAlignment,
                    Colour = Colour.ToArgb(),
                    DateTaken = DateTaken,
                    Filename = Filename,
                    FontBold = FontBold,
                    FontFamily = FontName,
                    FontSize = FontSize,
                    FontType = FontType,
                    ImageFormat = ImageFormat,
                    IsMetadataLoaded = false,
                    Latitude = Latitude,
                    Longitude = Longitude,
                    OutputFilename = filename,
                    Rotation = Rotation
                };
                _imageMetadataService.Save(metadata, Filename);

                // save the output file for the image
                _current.OutputFilename = filename;

                // do we need to flag it as saved?
                if (_current.IsSaved) return;

                // flag that the current image has metadata
                _current.IsMetadataLoaded = true;

                // flag that the current image has been saved
                _current.IsSaved = true;

                // flag that the current preview needs to be reloaded
                _current.IsPreviewLoaded = false;

                // save the quick caption
                _quickCaptionService.Add(metadata);

                // reload the preview
                Task.Factory.StartNew(() => PreviewThread(_current, _openCancellationTokenSource.Token),
                        _openCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current)
                    .ContinueWith(OnError, _openCancellationTokenSource.Token, TaskContinuationOptions.OnlyOnFaulted);

                _logService.Trace($"Checking if there is an image after position {Position}...");
                if (Position >= Count - 1) return;

                _logService.Trace($"There is an image after position {Position}.  Moving to next image...");
                Position++;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void UseBackgroundSecondColour()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace(
                    $"Setting {nameof(BackgroundColour)} to value of {nameof(BackgroundSecondColour)}...");
                BackgroundColour = BackgroundSecondColour ?? Color.Black;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        #region delegates

        private delegate void OnOpenedDelegate(string directory, int count);

        public delegate void OpenedEventHandler(object sender, OpenedEventArgs e);

        public delegate void PreviewLoadedEventHandler(object sender, PreviewLoadedEventArgs e);

        #endregion
    }
}