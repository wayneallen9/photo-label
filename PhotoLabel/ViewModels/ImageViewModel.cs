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
        private CancellationTokenSource _imageCancellationTokenSource;
        private string _caption;
        private CaptionAlignments? _captionAlignment = CaptionAlignments.TopLeft;
        private Color? _color;
        private volatile bool _exifLoaded = false;
        private string _filename;
        private Font _font;
        private bool _hasMetadata = false;
        private Image _image;
        private readonly IImageMetadataService _imageMetadataService;
        private readonly IImageService _imageService;
        private float? _latitude;
        private float? _longitude;
        private readonly ILogService _logService;
        private volatile bool _metadataLoaded = false;
        private Image _preview;
        private CancellationTokenSource _previewCancellationTokenSource;
        private Rotations _rotation = Rotations.Zero;
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

        public Color? Color
        {
            get => _color;
            set
            {
                // save the new value
                _color = value;

                // clear the image cache
                _image = null;

                OnPropertyChanged();
                OnPropertyChanged(nameof(Image));
            }
        }

        public string Caption
        {
            get => _caption;
            set
            {
                // save the new value
                _caption = value;

                // clear the image cache
                _image = null;

                // trigger the update
                OnPropertyChanged();
                OnPropertyChanged(nameof(Image));
            }
        }

        public CaptionAlignments? CaptionAlignment
        {
            get => _captionAlignment;
            set
            {// save the value
                _captionAlignment = value;

                // clear the image cache
                _image = null;

                OnPropertyChanged();
                OnPropertyChanged(nameof(Image));
            }
        }

        public string Filename
        {
            get => _filename; set
            {
                // save the new value
                _filename = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(FilenameWithoutPath));
                OnPropertyChanged(nameof(Image));
            }
        }

        public string FilenameWithoutPath
        {
            get => Path.GetFileName(Filename);
        }

        public Font Font
        {
            get => _font;
            set
            { //save the new value
                _font = value;

                // clear the image cache
                _image = null;

                OnPropertyChanged();
                OnPropertyChanged(nameof(Image));
            }
        }

        public Image LoadPreview()
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

                // which overlay do we use?
                if (Saved)
                    _preview = _imageService.Overlay(Filename, PreviewWidth, PreviewHeight, Properties.Resources.saved, PreviewWidth - Properties.Resources.saved.Width - 4, 4);
                else if (_imageMetadataService.HasMetadata(Filename))
                    _preview = _imageService.Overlay(Filename, PreviewWidth, PreviewHeight, Properties.Resources.metadata, PreviewWidth - Properties.Resources.metadata.Width - 4, 4);
                else
                    _preview = _imageService.Get(Filename, PreviewWidth, PreviewHeight);

                return _preview;
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
                // is the image cached?
                if (_image != null) return _image;

                // if there is an existing load in progress, cancel it
                if (_imageCancellationTokenSource != null) _imageCancellationTokenSource.Cancel();

                // create a new cancellation token
                _imageCancellationTokenSource = new CancellationTokenSource();

                // load the image on a new thread (for performance)
                Task.Factory.StartNew(() => ImageThread(_imageCancellationTokenSource.Token), TaskCreationOptions.LongRunning);

                return null;
            }
        }

        public float? Latitude
        {
            get => _latitude;
            set
            {
                // save the value
                _latitude = value;

                OnPropertyChanged();
            }
        }

        private void ImageThread(CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                // load the Exif data for the image (if required)
                if (cancellationToken.IsCancellationRequested) return;

                // we only want to load the Exif data once
                lock (this)
                {
                    // do we need to load the metadata for the image?
                    _logService.Trace($"Checking if metadata is loaded for image \"{Filename}\"...");
                    if (!_metadataLoaded)
                    {
                        _logService.Trace($"Loading metadata for image \"{Filename}\"...");
                        Caption = _imageMetadataService.LoadCaption(Filename);
                        CaptionAlignment = _imageMetadataService.LoadCaptionAlignment(Filename);
                        Color = _imageMetadataService.LoadColor(Filename);
                        Font = _imageMetadataService.LoadFont(Filename);
                        Rotation = _imageMetadataService.LoadRotation(Filename) ?? Rotations.Zero;

                        // don't load it again
                        _metadataLoaded = true;
                    }

                    // do we need to load the Exif data for the image?
                    _logService.Trace("Checking if Exif data is loaded for image \"{Filename}\"...");
                    if (!_exifLoaded)
                    {
                        _logService.Trace($"Loading Exif data for image \"{Filename}\"...");
                        var exifData = _imageService.GetExifData(Filename);

                        // set the caption (if it is not already set)
                        if (Caption == null) Caption = exifData.DateTaken;

                        // set the latitude and longitude (since we have just loaded them)
                        Latitude = exifData.Latitude;
                        Longitude = exifData.Longitude;

                        // don't load it again
                        _exifLoaded = true;
                    }
                }

                // set the properties required to draw the image
                if (cancellationToken.IsCancellationRequested) return;
                var caption = Caption ?? string.Empty;
                var captionAlignment = CaptionAlignment ?? CaptionAlignments.TopLeft;
                var color = Color ?? System.Drawing.Color.White;
                var font = Font ?? SystemFonts.DefaultFont;

                // load the image from disk
                if (cancellationToken.IsCancellationRequested) return;
                var originalImage = _imageService.Get(Filename);

                lock (originalImage)
                {
                    // now caption it
                    if (cancellationToken.IsCancellationRequested) return;
                    var image = _imageService.Caption(originalImage, caption, captionAlignment, font, new SolidBrush(color), Rotation);

                    // now update the image
                    if (cancellationToken.IsCancellationRequested) return;
                    _image = image;
                }

                // the image has been updated
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

        public Image Preview
        {
            get {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if \"{Filename}\" has a cached preview...");
                    if (_preview != null)
                    {
                        _logService.Trace($"\"{Filename}\" has a cached preview");
                        return _preview;
                    }

                    // is the preview already loading?
                    if (_previewCancellationTokenSource != null) _previewCancellationTokenSource.Cancel();

                    // create a new cancellation token
                    _previewCancellationTokenSource = new CancellationTokenSource();

                    Task.Factory.StartNew(() => PreviewThread(_previewCancellationTokenSource.Token), TaskCreationOptions.LongRunning);

                    return null;
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        private void PreviewThread(CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;

                // which overlay do we use?
                if (Saved)
                {
                    _logService.Trace($"\"{Filename}\" has been saved.  Rendering preview with saved overlay...");
                    _preview = _imageService.Overlay(Filename, PreviewWidth, PreviewHeight, Properties.Resources.saved, PreviewWidth - Properties.Resources.saved.Width - 4, 4);
                } 
                else if (_imageMetadataService.HasMetadata(Filename))
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
            get => _longitude; set
            {
                // save the new value
                _longitude = value;

                OnPropertyChanged();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Rotations Rotation
        {
            get => _rotation; set
            {
                // save the new value
                _rotation = value;

                // invalidate the image
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

                var original = _imageService.Get(Filename);
                lock (original)
                {
                    // create the image to save
                    var captioned = _imageService.Caption(original, Caption, CaptionAlignment.Value, Font, new SolidBrush(Color.Value), Rotation);

                    // save the image to the file
                    _logService.Trace($"Saving \"{targetFilename}\"...");
                    captioned.Save(targetFilename, ImageFormat.Jpeg);
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