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
        private Color? _color;
        private ExifData _exifData;
        private volatile bool _exifLoading = false;
        private readonly object _exifLock = new object();
        private Font _font;
        private Image _image;
        private readonly object _imageLock = new object();
        private CancellationTokenSource _imageCancellationTokenSource;
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
            get
            {
                // is there a custom caption?
                if (_caption != null) return _caption;

                // is there a caption in the metadata?
                if (Metadata?.Caption != null) return Metadata.Caption;

                if (ExifData?.DateTaken != null) return ExifData.DateTaken;

                return string.Empty;
            }
            set
            {
                // ignore non-changes
                if (_caption == value) return;

                // save the new value
                _caption = value;

                // clear the cached image
                _image = null;

                // trigger the update
                OnPropertyChanged();
                OnPropertyChanged(nameof(Image));
            }
        }

        public CaptionAlignments CaptionAlignment
        {
            get
            {
                // is there a custom alignment set?
                if (_captionAlignment != null) return _captionAlignment.Value;

                // return the value from the metadata
                if (Metadata?.CaptionAlignment != null) return Metadata.CaptionAlignment;

                return Properties.Settings.Default.CaptionAlignment;
            }
            set
            {
                // ignore non-changes
                if (_captionAlignment == value) return;

                // save the value
                _captionAlignment = value;

                // clear the cached image
                _image = null;

                OnPropertyChanged();
                OnPropertyChanged(nameof(Image));
            }
        }

        public Color Color
        {
            get
            {
                // has a custom color been set?
                if (_color != null) return _color.Value;

                // use the color from the metadata
                if (Metadata?.Color != null) return Metadata.Color;

                return Properties.Settings.Default.Color;
            }
            set
            {
                // ignore non-changes
                if (_color == value) return;

                // save the new value
                _color = value;

                // clear the cached image
                _image = null;

                OnPropertyChanged();
                OnPropertyChanged(nameof(Image));
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

        private ExifData ExifData
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
        }

        private void ExifDataThread()
        {
            _logService.TraceEnter();
            try
            {
                LoadExifData();

                // clear the cached image
                _image = null;

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
                    _logService.Trace($"Checking if another thread has loaded the Exif data for \"{Filename}\"...");
                    if (_exifData != null)
                    {
                        _logService.Trace($"Exif data for \"{Filename}\" has been loaded on another thread.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Loading Exif data for \"{Filename}\"...");
                    _exifData = _imageService.GetExifData(Filename);
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
            get
            {
                // has a custom font been set?
                if (_font != null) return _font;

                if (Metadata?.Font != null) return Metadata.Font;

                return Properties.Settings.Default.Font;
            }
            set
            {
                // ignore non-changes
                if (_font == value) return;

                //save the new value
                _font = value;

                // clear the cached image
                _image = null;

                OnPropertyChanged();
                OnPropertyChanged(nameof(Image));
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

        public Image Image
        {
            get
            {
                // if the image has been cached, return it
                if (_image != null) return _image;

                lock (_imageLock)
                {
                    // is the image already loading?
                    if (_imageCancellationTokenSource != null) _imageCancellationTokenSource.Cancel();

                    // create a new cancellation token
                    _imageCancellationTokenSource = new CancellationTokenSource();

                    // load the image on a new thread (for performance)
                    var thread = new Thread(ImageThread);
                    thread.Start(_imageCancellationTokenSource.Token);
                }

                return null;
            }
        }

        public float? Latitude
        {
            get => ExifData?.Latitude;
        }

        private void ImageThread(object state)
        {
            _logService.TraceEnter();
            try
            {
                if (!(state is CancellationToken cancellationToken)) return;

                if (cancellationToken.IsCancellationRequested) return;
                Thread.Sleep(100);

                if (cancellationToken.IsCancellationRequested) return;
                LoadMetadata();

                // we only need the Exif data if the metadata did not load
                if (cancellationToken.IsCancellationRequested) return;
                if (_metadata == null) LoadExifData();

                if (cancellationToken.IsCancellationRequested) return;
                _logService.Trace($"Adding caption to \"{Filename}\"...");
                _image = _imageService.Caption(Filename, Caption, CaptionAlignment, Font, new SolidBrush(Color), Rotation);

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

        private Metadata Metadata
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
                    _logService.Trace($"Checking if another thread has loaded the metadata for \"{Filename}\"...");
                    if (_metadata != null)
                    {
                        _logService.Trace($"Metadata for \"{Filename}\" has been loaded on another thread.  Exiting...");
                        return;
                    }

                    // load the metadata
                    _logService.Trace($"Loading metadata for \"{Filename}\" from disk...");
                    _metadata = _imageMetadataService.Load(Filename);
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

                if (cancellationToken.IsCancellationRequested) return;
                OnPropertyChanged(nameof(Preview));
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public float? Longitude
        {
            get => ExifData?.Longitude;
        }

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
                OnPropertyChanged(nameof(Color));
                OnPropertyChanged(nameof(Font));

                // clear the image cache
                _image = null;
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
            get
            {
                // has a custom rotation been set?
                if (_rotation != null) return _rotation.Value;

                if (Metadata?.Rotation != null) return Metadata.Rotation;

                return Rotations.Zero;
            }
            set
            {
                // ignore non-changes
                if (_rotation == value) return;

                // save the new value
                _rotation = value;

                // clear the cached image
                _image = null;

                OnPropertyChanged();
                OnPropertyChanged(nameof(Image));
            }
        }

        public bool Save(string outputFolder, bool overwriteIfExists)
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
                lock (this)
                {
                    // draw the caption on it
                    var captioned = _imageService.Caption(Filename, Caption, CaptionAlignment, Font, new SolidBrush(Color), Rotation);

                    // save the image to the file
                    _logService.Trace($"Saving \"{targetFilename}\"...");
                    captioned.Save(targetFilename, ImageFormat.Jpeg);

                    // do we need to create a new metadata file?
                    if (_metadata == null)
                        _metadata = new Metadata
                        {
                            Caption = Caption,
                            CaptionAlignment = CaptionAlignment,
                            Color = Color,
                            Font = Font,
                            Rotation = Rotation
                        };
                    else
                    {
                        _metadata.Caption = Caption;
                        _metadata.CaptionAlignment = CaptionAlignment;
                        _metadata.Color = Color;
                        _metadata.Font = Font;
                        _metadata.Rotation = Rotation;
                    }

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