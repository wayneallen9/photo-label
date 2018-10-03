using PhotoLabel.Services;
using PhotoLabel.Services.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
namespace PhotoLabel.ViewModels
{
    public class ImageViewModel : IDisposable, IObservable<ImageViewModel>
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
        private bool _exifLoaded;
        private readonly object _exifLock = new object();
        private Font _font;
        private readonly object _fontLock = new object();
        private Image _image;
        private readonly object _imageLock = new object();
        private CancellationTokenSource _imageCancellationTokenSource;
        private readonly object _imageLoaderLock = new object();
        private readonly IImageMetadataService _imageMetadataService;
        private readonly IImageService _imageService;
        private float? _latitude;
        private readonly ILogService _logService;
        private Metadata _metadata;
        private bool _metadataLoaded;
        private readonly object _metadataLock = new object();
        private readonly IList<IObserver<ImageViewModel>> _observers;
        private readonly object _previewLock = new object();
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

            // initialise variables
            _exifLoaded = false;
            _metadataLoaded = false;
            _observers = new List<IObserver<ImageViewModel>>();
        }

        private void CancelLoadImage()
        {
            _logService.TraceEnter();
            try
            {
                lock (_imageLock) { 
                    _logService.Trace("Cancelling any in progress image load...");
                    _imageCancellationTokenSource?.Cancel();

                    _logService.Trace("Releasing existing image memory...");
                    _image?.Dispose();
                    _image = null;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
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

                // reload the image
                _image = null;

                // trigger the update
                Notify();
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
                _image = null;

                Notify();
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
                _image = null;

                Notify();
            }
        }

        private void ExceptionHandler(Task task)
        {
            _logService.TraceEnter();
            try
            {
                NotifyError(task.Exception);
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
                // only process changes
                if (Font == value) return;

                //save the new value in a thread safe manner
                lock (_fontLock)
                    _font = value;

                // clear the cached image
                _image = null;

                Notify();
            }
        }

        private void LoadExifData()
        {
            _logService.TraceEnter();
            try
            {
                // this method needs to be thread safe
                _logService.Trace("Creating a thread-safe lock for Exif data...");
                lock (_exifLock)
                {
                    _logService.Trace("Checking if Exit data is already loaded...");
                    if (_exifLoaded)
                    {
                        _logService.Trace("Exif data is already loaded.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Checking if another thread has loaded the Exif data for \"{Filename}\"...");
                    if (_exifData != null)
                    {
                        _logService.Trace($"Exif data for \"{Filename}\" has been loaded on another thread.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Loading Exif data for \"{Filename}\"...");
                    _exifData = _imageService.GetExifData(Filename);

                    // flag that the Exif data is loaded
                    _exifLoaded = true;

                    // populate the values
                    lock (_captionLock)
                        if (Caption == null)
                        {
                            _caption = _exifData.DateTaken;
                        }

                    _latitude = _exifData.Latitude;
                    Longitude = _exifData.Longitude;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void LoadPreview(CancellationToken cancellationToken, TaskCreationOptions taskCreationOptions)
        {
            _logService.TraceEnter();
            try
            {
                // start loading the preview in the thread pool
                var task = new Task(PreviewThread, cancellationToken, taskCreationOptions);
                task.ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
                task.Start();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Image Image => _image;

        public float? Latitude => _latitude;

        private void LoadImageThread(CancellationToken cancellationToken, CaptionAlignments defaultCaptionAlignment, Color defaultColour, Font defaultFont)
        {
            _logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                LoadMetadata();

                // we only need the Exif data if the metadata did not load
                if (cancellationToken.IsCancellationRequested) return;
                if (_metadata != null)
                    LoadFromMetadata();
                else
                    LoadExifData();
                if (cancellationToken.IsCancellationRequested) return;

                lock (_imageLoaderLock)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    _logService.Trace($"Adding caption to \"{Filename}\"...");
                    var image = _imageService.Caption(Filename, _caption ?? string.Empty, _captionAlignment ?? defaultCaptionAlignment, _font ?? defaultFont, new SolidBrush(_colour ?? defaultColour), Rotation);

                    if (cancellationToken.IsCancellationRequested) return;
                    _image = image;
                }

                Notify();
                NotifyImage();
            }
            catch (Exception ex)
            {
                // log the error
                _logService.Error(ex);

                NotifyError(ex);
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
                     _imageCancellationTokenSource?.Cancel();

                    // create a new cancellation token
                    _imageCancellationTokenSource = new CancellationTokenSource();

                    // load the image on a new thread (for performance)
                    Task.Delay(100, _imageCancellationTokenSource.Token)
                        .ContinueWith((t, o) => LoadImageThread(_imageCancellationTokenSource.Token, defaultCaptionAlignment, defaultColor, defaultFont), _imageCancellationTokenSource.Token, TaskContinuationOptions.LongRunning)
                        .ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void LoadMetadata()
        {
            _logService.TraceEnter();
            try
            {
                // this method needs to be thread safe
                _logService.Trace("Creating a thread-safe lock for metadata...");
                lock (_metadataLock)
                {
                    _logService.Trace("Checking if metadata is already loaded...");
                    if (_metadataLoaded)
                    {
                        _logService.Trace("Metadata is already loaded.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Checking if another thread has loaded the metadata for \"{Filename}\"...");
                    if (_metadata != null)
                    {
                        _logService.Trace($"Metadata for \"{Filename}\" has been loaded on another thread.  Exiting...");
                        return;
                    }

                    // load the metadata
                    _logService.Trace($"Loading metadata for \"{Filename}\" from disk...");
                    _metadata = _imageMetadataService.Load(Filename);

                    // flag that the metadata has already been loaded
                    _metadataLoaded = true;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void LoadFromMetadata()
        {
            _logService.TraceEnter();
            try
            {
                // populate the properties
                _logService.Trace($"Setting caption to \"{_metadata.Caption}\"...");
                lock (_captionLock)
                    if (Caption == null)
                    {
                        Caption = _metadata.Caption;
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

                _latitude = _metadata.Latitude;
                Longitude = _metadata.Longitude;
                OnPropertyChanged(nameof(Longitude));

                lock (_rotationLock)
                    if (_rotation == null)
                    {
                        _rotation = _metadata.Rotation;
                        OnPropertyChanged(nameof(Rotation));
                    }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Image Preview { get; private set; }

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
                        Preview = _imageService.Overlay(Filename, PreviewWidth, PreviewHeight, Properties.Resources.saved, PreviewWidth - Properties.Resources.saved.Width - 4, 4);
                    }
                    else if (_metadata != null)
                        Preview = _imageService.Overlay(Filename, PreviewWidth, PreviewHeight, Properties.Resources.metadata, PreviewWidth - Properties.Resources.metadata.Width - 4, 4);
                    else
                        Preview = _imageService.Get(Filename, PreviewWidth, PreviewHeight);
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

        private void Notify()
        {
            _logService.TraceEnter();
            try
            {
                foreach (var observer in _observers)
                    observer.OnUpdate(this);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void NotifyError(Exception ex)
        {
            _logService.TraceEnter();
            try
            {
                foreach (var observer in _observers)
                    observer.OnError(ex);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void NotifyImage()
        {
            _logService.TraceEnter();
            try
            {
                foreach (var observer in _imageObservers)
                    observer.OnImage(this);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void NotifyPrevie()
        {
            _logService.TraceEnter();
            try
            {
                foreach (var observer in _observers)
                    observer.OnPreview(this);
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
                _image = null;

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

                    lock (_metadataLock)
                    {
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
                Preview = null;

                OnPropertyChanged();
            }
        }

        #region IObservable support
        public IDisposable Subscribe(IObserver<ImageViewModel> observer)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if observer is already observing...");
                if (!_observers.Contains(observer))
                {
                    _logService.Trace("Observer is not already observing.  Adding observer...");
                    _observers.Add(observer);

                    // update with the initial values
                    observer.OnImage(this);
                    observer.OnPreview(this);
                    observer.OnUpdate(this);
                }

                return new Unsubscriber<ImageViewModel>(_observers, observer);
            }
            finally
            {
                _logService.TraceExit();
            }
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    CancelLoadImage();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }

        public IDisposable Subscribe(IObserver observer)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}