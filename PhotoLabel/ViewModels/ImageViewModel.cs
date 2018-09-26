using PhotoLabel.Services;
using PhotoLabel.Services.Models;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
namespace PhotoLabel.ViewModels
{
    public class ImageViewModel : INotifyPropertyChanged
    {
        #region constants
        private const int PreviewHeight = 128;
        private const int PreviewWidth = 128;
        #endregion

        #region delegates
        #endregion

        #region events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region variables
        private string _caption;
        private CaptionAlignments? _captionAlignment;
        private readonly object _captionAlignmentLock = new object();
        private readonly object _captionLock = new object();
        private Color? _colour;
        private readonly object _colourLock = new object();
        private ExifData _exifData;
        private volatile bool _exifLoading = false;
        private readonly object _exifLock = new object();
        private Font _font;
        private readonly object _fontLock = new object();
        private readonly object _imageLock = new object();
        private CancellationTokenSource _imageCancellationTokenSource;
        private readonly object _imageLoaderLock = new object();
        private readonly IImageMetadataService _imageMetadataService;
        private readonly IImageService _imageService;
        private Exception _lastException;
        private readonly ILogService _logService;
        private Metadata _metadata;
        private volatile bool _metadataLoading = false;
        private readonly object _metadataLock = new object();
        private Image _preview;
        private readonly object _previewLock = new object();
        private CancellationTokenSource _previewCancellationTokenSource;
        private Rotations? _rotation;
        private readonly object _rotationLock = new object();
        private bool _saved = false;
        #endregion

        public ImageViewModel(
            IImageMetadataService imageMetadataService,
            IImageService imageService,
            ILogService logService)
        {
            // save the injections
            _imageMetadataService = imageMetadataService;
            _imageService = imageService;
            _logService = logService;
        }

        public string Caption
        {
            get => _caption;
            set
            {
                // ignore non-changes
                if (_caption == value) return;

                // save the change in a thread-safe manner
                lock (_metadataLock)
                    _caption = value;

                // clear the cached image
                Image = null;

                // trigger the update
                OnPropertyChanged();
            }
        }

        public CaptionAlignments? CaptionAlignment
        {
            get => _captionAlignment;
            set
            {
                // ignore non-changes
                if (_captionAlignment == value) return;

                // save the value in a thread safe manner
                lock (_captionAlignmentLock)
                    _captionAlignment = value;

                // clear the cached image
                Image = null;

                OnPropertyChanged();
            }
        }

        public Color? Colour
        {
            get => _colour;
            set
            {
                // ignore non-changes
                if (_colour == value) return;

                // save the new value in a thread safe manner
                lock (_colourLock)
                    _colour = value;

                // clear the cached image
                Image = null;

                OnPropertyChanged();
            }
        }

        private void ExceptionHandler(Task task)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Getting exception...");
                _lastException = task.Exception;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        /*private ExifData ExifData
        {
            get
            {
                // has the data already been loaded?
                if (_exifData != null) return _exifData;

                // is it already being loaded from disk?
                if (_exifLoading) return null;

                // flag that the exif is now loading
                _exifLoading = true;

                // load the data from disk on another thread
                var task = new Task(ExifDataThread, TaskCreationOptions.LongRunning);
                task.ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
                task.Start();

                return null;
            }
        }*/

        /*private void ExifDataThread()
        {
            _logService.TraceEnter();
            try
            {
                LoadExifData();

                // clear the cached image
                Image = null;

                // update the properties
                OnPropertyChanged(nameof(Caption));
                OnPropertyChanged(nameof(Image));
                OnPropertyChanged(nameof(Latitude));
                OnPropertyChanged(nameof(Longitude));
            }
            finally
            {
                _logService.TraceExit();
            }
        }*/

        private void LoadExifData()
        {
            _logService.TraceEnter();
            try
            {
                // this method needs to be thread safe
                _logService.Trace("Creating a thread-safe lock for Exif data...");
                lock (_exifLock)
                {
                    _logService.Trace($"Checking if another thread has loaded the Exif data for \"{Filename}\"...");
                    if (_exifData != null)
                    {
                        _logService.Trace($"Exif data for \"{Filename}\" has been loaded on another thread.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Loading Exif data for \"{Filename}\"...");
                    _exifData = _imageService.GetExifData(Filename);

                    // populate the values
                    lock (_captionLock)
                        if (_caption == null)
                        {
                            _caption = _exifData.DateTaken;
                            OnPropertyChanged(nameof(Caption));
                        }

                    Latitude = _exifData.Latitude;
                    OnPropertyChanged(nameof(Latitude));
                    Longitude = _exifData.Longitude;
                    OnPropertyChanged(nameof(Longitude));
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public string Filename { get; set; }

        public string FilenameWithoutPath
        {
            get => Path.GetFileName(Filename);
        }

        public Font Font
        {
            get => _font;
            set
            {
                // ignore non-changes
                if (_font == value) return;

                //save the new value in a thread safe manner
                lock (_fontLock)
                    _font = value;

                // clear the cached image
                Image = null;

                OnPropertyChanged();
            }
        }

        public void GetPreview()
        {
            _logService.TraceEnter();
            try
            {
                // create the cancellation token for this preview
                var cancellationTokenSource = new CancellationTokenSource();

                // start loading the preview in the thread pool
                ThreadPool.QueueUserWorkItem(PreviewThread, cancellationTokenSource.Token);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Image Image { get; private set; }

        public float? Latitude { get; private set; }

        private void ImageThread(object state)
        {
            _logService.TraceEnter();
            try
            {
                if (!(state is object[] stateParams)) return;

                // get the parameters
                if (!(stateParams[0] is CancellationToken cancellationToken)) return;
                if (!(stateParams[2] is Color defaultColour)) return;
                if (!(stateParams[3] is Font defaultFont)) return;

                // get the default alignment
                var defaultCaptionAlignment = (CaptionAlignments)stateParams[1];

                if (cancellationToken.IsCancellationRequested) return;
                Thread.Sleep(100);

                if (cancellationToken.IsCancellationRequested) return;
                LoadMetadata();

                // we only need the Exif data if the metadata did not load
                if (cancellationToken.IsCancellationRequested) return;
                if (_metadata == null) LoadExifData();

                lock (_imageLoaderLock)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    _logService.Trace($"Adding caption to \"{Filename}\"...");
                    Image = _imageService.Caption(Filename, _caption, _captionAlignment ?? defaultCaptionAlignment, _font ?? defaultFont, new SolidBrush(_colour ?? defaultColour), Rotation);
                }

                // the image has been updated
                if (cancellationToken.IsCancellationRequested) return;
                OnPropertyChanged(nameof(Image));
            }
            catch (Exception ex)
            {
                _logService.Error(ex);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void LoadImage(CaptionAlignments defaultCaptionAlignment, Color defaultColor, Font defaultFont)
        {
            _logService.TraceEnter();
            try
            {
                lock (_imageLock)
                {
                    // is the image already loading?
                    if (_imageCancellationTokenSource != null) _imageCancellationTokenSource.Cancel();

                    // create a new cancellation token
                    _imageCancellationTokenSource = new CancellationTokenSource();

                    // load the image on a new thread (for performance)
                    var thread = new Thread(ImageThread);
                    thread.Start(new object[] { _imageCancellationTokenSource.Token, defaultCaptionAlignment, defaultColor, defaultFont });
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        /*private Metadata Metadata
        {
            get
            {
                // has it already been cached?
                if (_metadata != null) return _metadata;

                // is it already loading?
                if (_metadataLoading) return null;

                // flag that it is loading
                _metadataLoading = true;

                // load the metadata
                var task = new Task(MetadataThread);
                task.ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
                task.Start();

                return null;
            }
        }*/

        private void LoadMetadata()
        {
            _logService.TraceEnter();
            try
            {
                // this method needs to be thread safe
                _logService.Trace("Creating a thread-safe lock for metadata...");
                lock (_metadataLock)
                {
                    _logService.Trace($"Checking if another thread has loaded the metadata for \"{Filename}\"...");
                    if (_metadata != null)
                    {
                        _logService.Trace($"Metadata for \"{Filename}\" has been loaded on another thread.  Exiting...");
                        return;
                    }

                    // load the metadata
                    _logService.Trace($"Loading metadata for \"{Filename}\" from disk...");
                    _metadata = _imageMetadataService.Load(Filename);

                    // if it didn't load, there is nothing else
                    if (_metadata == null) return;

                    // populate the properties
                    lock (_captionLock)
                        if (_caption == null)
                        {
                            _caption = _metadata.Caption;
                            OnPropertyChanged(nameof(Caption));
                        }

                    lock (_captionAlignmentLock)
                        if (_captionAlignment == null)
                        {
                            _captionAlignment = _metadata.CaptionAlignment;
                            OnPropertyChanged(nameof(CaptionAlignment));
                        }

                    lock (_colourLock)
                        if (_colour == null)
                        {
                            _colour = Color.FromArgb(_metadata.Color);
                            OnPropertyChanged(nameof(Colour));
                        }

                    lock (_fontLock)
                        if (_font == null)
                        {
                            _font = new Font(_metadata.FontFamily, _metadata.FontSize, _metadata.FontBold ? FontStyle.Bold : FontStyle.Regular);
                            OnPropertyChanged(nameof(Font));
                        }

                    Latitude = _metadata.Latitude;
                    OnPropertyChanged(nameof(Latitude));
                    Longitude = _metadata.Longitude;
                    OnPropertyChanged(nameof(Longitude));

                    lock (_rotationLock)
                        if (_rotationLock == null)
                        {
                            _rotation = _metadata.Rotation;
                            OnPropertyChanged(nameof(Rotation));
                        }
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Image Preview
        {
            get
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if \"{Filename}\" has a cached preview...");
                    if (_preview != null)
                    {
                        _logService.Trace($"\"{Filename}\" has a cached preview");
                        return _preview;
                    }

                    lock (_previewLock)
                    {
                        // is the preview already loading?
                        if (_previewCancellationTokenSource != null) _previewCancellationTokenSource.Cancel();

                        // create a new cancellation token
                        _previewCancellationTokenSource = new CancellationTokenSource();

                        // load it on a thread
                        var thread = new Thread(PreviewThread);
                        thread.Start(_previewCancellationTokenSource.Token);
                    }

                    return null;
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        private void PreviewThread(object state)
        {
            _logService.TraceEnter();
            try
            {
                if (!(state is CancellationToken cancellationToken)) return;

                if (cancellationToken.IsCancellationRequested) return;
                Thread.Sleep(100);

                // load the metadata from the disk
                if (cancellationToken.IsCancellationRequested) return;
                LoadMetadata();

                lock (_imageLoaderLock)
                {
                    // which overlay do we use?
                    if (cancellationToken.IsCancellationRequested) return;
                    if (Saved)
                    {
                        _logService.Trace($"\"{Filename}\" has been saved.  Rendering preview with saved overlay...");
                        _preview = _imageService.Overlay(Filename, PreviewWidth, PreviewHeight, Properties.Resources.saved, PreviewWidth - Properties.Resources.saved.Width - 4, 4);
                    }
                    else if (_metadata != null)
                        _preview = _imageService.Overlay(Filename, PreviewWidth, PreviewHeight, Properties.Resources.metadata, PreviewWidth - Properties.Resources.metadata.Width - 4, 4);
                    else
                        _preview = _imageService.Get(Filename, PreviewWidth, PreviewHeight);
                }

                if (cancellationToken.IsCancellationRequested) return;
                OnPropertyChanged(nameof(Preview));
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public float? Longitude { get; private set; }

        private void MetadataThread()
        {
            _logService.TraceEnter();
            try
            {
                // load the metadata
                LoadMetadata();

                // update the properties
                OnPropertyChanged(nameof(Caption));
                OnPropertyChanged(nameof(CaptionAlignment));
                OnPropertyChanged(nameof(Colour));
                OnPropertyChanged(nameof(Font));

                // clear the image cache
                Image = null;
                OnPropertyChanged(nameof(Image));

                // clear the preview cache
                _preview = null;
                OnPropertyChanged(nameof(Preview));
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Rotations Rotation
        {
            get => _rotation ?? Rotations.Zero;
            set
            {
                // ignore non-changes
                if (_rotation == value) return;

                // save the new value in a thread safe manner
                lock (_rotationLock)
                    _rotation = value;

                // clear the cached image
                Image = null;

                OnPropertyChanged();
            }
        }

        public bool Save(string outputFolder, bool overwriteIfExists, CaptionAlignments defaultCaptionAlignment, Color defaultColor, Font defaultFont)
        {
            _logService.TraceEnter();
            try
            {
                // create the target filename
                var filenameWithoutPath = Path.GetFileName(Filename);
                var targetFilename = Path.Combine(outputFolder, filenameWithoutPath);

                _logService.Trace($"Checking if \"{targetFilename}\" already exists...");
                if (File.Exists(targetFilename))
                {
                    _logService.Trace($"\"{targetFilename}\" already exists");
                    if (!overwriteIfExists) return false;
                }
                _logService.Trace($"\"{targetFilename}\" will be overwritten");

                // make the save thread safe
                lock (_captionLock)
                {
                    // populate with defaults where required
                    if (CaptionAlignment == null) CaptionAlignment = defaultCaptionAlignment;
                    if (Colour == null) Colour = defaultColor;
                    if (Font == null) Font = defaultFont;

                    // draw the caption on it
                    var captioned = _imageService.Caption(Filename, Caption, CaptionAlignment.Value, _font ?? defaultFont, new SolidBrush(_colour.Value), Rotation);

                    // save the image to the file
                    _logService.Trace($"Saving \"{targetFilename}\"...");
                    captioned.Save(targetFilename, ImageFormat.Jpeg);

                    // do we need to create a new metadata file?
                    if (_metadata == null) _metadata = new Metadata();

                    // set the properties
                    _metadata.Caption = _caption;
                    _metadata.CaptionAlignment = _captionAlignment.Value;
                    _metadata.Color = _colour.Value.ToArgb();
                    _metadata.FontBold = _font.Bold;
                    _metadata.FontFamily = _font.FontFamily.Name;
                    _metadata.FontSize = _font.Size;
                    _metadata.Latitude = Latitude;
                    _metadata.Longitude = Longitude;
                    _metadata.Rotation = Rotation;

                    _logService.Trace($"Saving metadata for \"{Filename}\"...");
                    _imageMetadataService.Save(_metadata, Filename);
                }

                // flag that the image has been saved
                Saved = true;

                return true;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public bool Saved
        {
            get => _saved; set
            {
                // save the new value
                _saved = value;

                // clear the preview
                _preview = null;

                OnPropertyChanged();
                OnPropertyChanged(nameof(Preview));
            }
        }
    }
}