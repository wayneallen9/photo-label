using PhotoLabel.Services;
using PhotoLabel.Services.Models;
using PhotoLabel.Wpf.Extensions;
using PhotoLabel.Wpf.Properties;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;

namespace PhotoLabel.Wpf
{
    public class ImageViewModel : IDisposable, INotifyPropertyChanged
    {
        #region constants
        private const double Tolerance = double.Epsilon;

        private const int PreviewHeight = 128;
        private const int PreviewWidth = 128;
        #endregion

        #region delegates

        private delegate void CaptionSuccessDelegate(Task<Bitmap> task, object state);
        private delegate BitmapSource CreateOpeningBitmapSourceDelegate();
        private delegate void LoadExifDataSuccessDelegate(Task<ExifData> task, object state);
        private delegate void LoadImageSuccessDelegate(Task<Image> task, object state);
        private delegate void LoadMetadataSuccessDelegate(Task<Metadata> task, object state);
        private delegate void LoadPreviewSuccessDelegate(Task<Bitmap> task, object state);
        private delegate void OnPropertyChangedDelegate(string propertyName = "");
        private delegate void UpdateExifDataDelegate(ExifData exifData);
        private delegate void UpdateImageDelegate(Bitmap image);
        private delegate void UpdateMetadataDelegate(Metadata metadata);
        private delegate void UpdatePreviewDelegate(Bitmap image);
        #endregion

        #region events

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region variables

        private static BitmapSource _openingBitmapSource;

        private Color? _backColor;
        private string _caption;
        private CaptionAlignments _captionAlignment;
        private bool _disposedValue;
        private readonly IConfigurationService _configurationService;
        private bool _fontBold;
        private string _fontName;
        private float? _fontSize;
        private string _fontType;
        private Color? _foreColor;
        private BitmapWrapper _imageWrapper;
        private Stretch _imageStretch;
        private bool _isEdited;
        private readonly ILogService _logService;
        private readonly object _metadataLock;
        private readonly ManualResetEvent _metadataManualResetEvent;
        private Image _originalImage;
        private readonly object _originalImageLock;
        private readonly ManualResetEvent _originalImageManualResetEvent;
        private BitmapWrapper _previewWrapper;
        private ICommand _rotateLeftCommand;
        private ICommand _rotateRightCommand;
        private Rotations? _rotation;
        #endregion

        public ImageViewModel(string filename)
        {
            // save dependencies
            Filename = filename;

            // get dependencies
            _configurationService = NinjectKernel.Get<IConfigurationService>();
            TaskScheduler = NinjectKernel.Get<SingleTaskScheduler>();
            _logService = NinjectKernel.Get<ILogService>();

            // initialise properties
            _fontType = "%";
            _imageStretch = Stretch.None;
            _metadataLock = new object();
            _metadataManualResetEvent = new ManualResetEvent(false);
            _originalImageLock = new object();
            _originalImageManualResetEvent = new ManualResetEvent(true);
            _rotation = Rotations.Zero;

            // load metadata on a low priority background thread
            LoadPreviewCancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(() => LoadPreviewThread(LoadPreviewCancellationTokenSource.Token),
                    LoadPreviewCancellationTokenSource.Token, TaskCreationOptions.None, TaskScheduler);
        }

        private static BitmapSource GetOpeningBitmapSource()
        {
            // create dependencies
            var loggingService = NinjectKernel.Get<ILogService>();

            loggingService.TraceEnter();
            try
            {
                loggingService.Trace("Checking if opening bitmap source has been created...");
                if (_openingBitmapSource != null)
                {
                    loggingService.Trace("Opening bitmap source has already been created.  Returning...");
                    return _openingBitmapSource;
                }

                loggingService.Trace("Checking if running on UI thread...");
                if (Application.Current.CheckAccess() == false)
                {
                    loggingService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    return (BitmapSource) Application.Current.Dispatcher.Invoke(
                        new CreateOpeningBitmapSourceDelegate(GetOpeningBitmapSource), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                }

                loggingService.Trace("Creating opening bitmap source...");
                _openingBitmapSource = Imaging.CreateBitmapSourceFromHBitmap(Resources.opening.GetHbitmap(),
                    IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                return _openingBitmapSource;
            }
            catch (Exception ex)
            {
                ex.ToString();

                return null;
            }
            finally
            {
                loggingService.TraceExit();
            }
        }

        public Color BackColor
        {
            get => _backColor ?? _configurationService.BackgroundColour.ToWindowsMediaColor();
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(BackColor)} has changed...");
                    if (_backColor == value)
                    {
                        _logService.Trace($"Value of {nameof(BackColor)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($@"Setting value of {nameof(BackColor)} to ""{value}""...");
                    _backColor = value;

                    _logService.Trace($@"Saving back color as default...");
                    _configurationService.BackgroundColour = value.ToDrawingColor();

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsBackColorEdited = true;
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BackColorOpacity));
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public string BackColorOpacity
        {
            get => _backColor.HasValue ? $"{_backColor?.A / 256d * 100, 0:F0}%" : "Off";
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($@"Checking if background has been set for ""{Filename}""...");
                    if (!_backColor.HasValue)
                    {
                        _logService.Trace($@"Background has not been set for ""{Filename}"".  Exiting...");
                        return;
                    }

                    _logService.Trace($@"Setting background opacity to ""{value}""...");
                    byte opacityValue;
                    switch (value)
                    {
                        case "Off":
                            opacityValue = 255;

                            break;
                        default:
                            var percentage = value.ToPercentage();
                            opacityValue = (byte)(percentage / 100 * 255);

                            break;
                    }

                    _logService.Trace("Checking if background opacity has changed...");
                    if (_backColor.Value.A == opacityValue)
                    {
                        _logService.Trace("Background opacity has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace("Setting background opacity...");
                    _backColor = Color.FromArgb(opacityValue, _backColor.Value.R,
                        _backColor.Value.G, _backColor.Value.B);

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BackColor));
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public string Caption
        {
            get => _caption;
            set
            {
                var stopwatch = Stopwatch.StartNew();

                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(Caption)} has changed...");
                    if (_caption == value)
                    {
                        _logService.Trace($"Value of {nameof(Caption)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($@"Setting value of {nameof(Caption)} to ""{value}""...");
                    _caption = value;

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsCaptionEdited = true;
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit(stopwatch);
                }
            }
        }

        public CaptionAlignments CaptionAlignment
        {
            get => _captionAlignment;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(CaptionAlignment)} has changed...");
                    if (_captionAlignment == value)
                    {
                        _logService.Trace($"Value of {nameof(CaptionAlignment)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(CaptionAlignment)} to {value}...");
                    _captionAlignment = value;

                    _logService.Trace($@"Saving caption alignment as default...");
                    _configurationService.CaptionAlignment = value;

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsCaptionAlignmentEdited = true;
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        private CancellationTokenSource CaptionCancellationTokenSource { get; set; }

        private void LoadImageThread(object state)
        {
            Bitmap captionedImage;

            var cancellationToken = (CancellationToken) state;
            
            // create dependencies
            var imageService = NinjectKernel.Get<IImageService>();
            var logService = NinjectKernel.Get<ILogService>();

            // initialise variables
            var stopwatch = Stopwatch.StartNew();

            logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                using (var originalImage = OriginalImage)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace($@"Checking if metadata for ""{Filename}"" has already been loaded...");
                    LoadMetadataThread(cancellationToken);

                    if (cancellationToken.IsCancellationRequested) return;
                    var brush = new SolidBrush(ForeColor.ToDrawingColor());

                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace($@"Captioning ""{Filename}"" with ""{Caption}""...");
                    captionedImage = imageService.Caption(originalImage, Caption, CaptionAlignment, FontName, FontSize, FontType,
                        FontBold, brush, BackColor.ToDrawingColor(), cancellationToken);
                    try
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        UpdateImage(captionedImage);
                    }
                    finally
                    {
                        captionedImage?.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            finally
            {
                logService.TraceExit(stopwatch);
            }
        }

        public bool FontBold
        {
            get => _fontBold;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(FontBold)} has changed...");
                    if (_fontBold == value)
                    {
                        _logService.Trace($"Value of {nameof(FontBold)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($@"Setting value of {nameof(FontBold)} to ""{value}""...");
                    _fontBold = value;

                    _logService.Trace($@"Saving font bold as default...");
                    _configurationService.FontBold = value;

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsFontBoldEdited = true;
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public string Filename { get; }

        public string FontName
        {
            get => _fontName ?? _configurationService.FontName;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(FontName)} has changed...");
                    if (_fontName == value)
                    {
                        _logService.Trace($"Value of {nameof(FontName)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($@"Setting value of {nameof(FontName)} to ""{value}""...");
                    _fontName = value;

                    _logService.Trace($@"Saving ""{value}"" as default font...");
                    _configurationService.FontName = value;

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsFontNameEdited = true;
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public float FontSize
        {
            get => _fontSize ?? _configurationService.FontSize;
            set
            {
                _logService.Trace("Checking if value is greater than 0...");
                if (value <= 0f) throw new ArgumentOutOfRangeException();

                _logService.Trace($"Checking if value of {nameof(FontSize)} has changed...");
                if (Math.Abs(FontSize - value) < Tolerance)
                {
                    _logService.Trace($"Value of {nameof(FontSize)} has not changed.  Exiting...");
                    return;
                }

                _logService.Trace($"Setting value of {nameof(FontSize)} to {value}...");
                _fontSize = value;

                _logService.Trace($@"Saving font size {value} as default...");
                _configurationService.FontSize = value;

                _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                IsFontSizeEdited = true;
                IsEdited = true;

                _logService.Trace($@"Loading image for ""{Filename}""...");
                LoadImage();

                OnPropertyChanged();
            }
        }

        public string FontType
        {
            get => _fontType ?? _configurationService.FontType;
            set
            {
                _logService.TraceEnter();
                try
                {
                    if (value != "%" && value != "pts") throw new ArgumentOutOfRangeException(nameof(FontType));

                    _logService.Trace($"Checking if value of {nameof(FontType)} has changed...");
                    if (_fontType == value)
                    {
                        _logService.Trace($"Value of {nameof(FontType)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($@"Setting value of {nameof(FontType)} to ""{value}""...");
                    _fontType = value;

                    _logService.Trace($@"Saving font type ""{value}"" as default...");
                    _configurationService.FontType = value;

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsFontTypeEdited = true;
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public Color ForeColor
        {
            get => _foreColor ?? _configurationService.Colour.ToWindowsMediaColor();
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(ForeColor)} has changed...");
                    if (_foreColor == value)
                    {
                        _logService.Trace($"Value of {nameof(ForeColor)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($@"Setting value of {nameof(ForeColor)} to ""{value}""...");
                    _foreColor = value;

                    _logService.Trace($@"Saving as default fore color...");
                    _configurationService.Colour = value.ToDrawingColor();

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsForeColorEdited = true;
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        private bool HasMetadata { get; set; }

        public BitmapSource Image => _imageWrapper?.BitmapSource ?? GetOpeningBitmapSource();

        public Stretch ImageStretch
        {
            get => _imageStretch;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(ImageStretch)} has changed...");
                    if (_imageStretch == value)
                    {
                        _logService.Trace($"Value of {nameof(ImageStretch)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(ImageStretch)} to {value}...");
                    _imageStretch = value;

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        private bool IsBackColorEdited { get; set; }

        private bool IsCaptionAlignmentEdited { get; set; }

        private bool IsCaptionEdited { get; set; }

        private bool IsEdited
        {
            get => _isEdited;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(IsEdited)} has changed...");
                    if (_isEdited == value)
                    {
                        _logService.Trace($"Value of {nameof(IsEdited)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(IsEdited)} to {value}...");
                    _isEdited = value;

                    _logService.Trace($@"Reloading preview image for ""{Filename}""...");
                    LoadPreview(new CancellationToken());
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        private bool IsExifLoaded { get; set; }

        private bool IsFontBoldEdited { get; set; }

        private bool IsFontNameEdited { get; set; }

        private bool IsFontSizeEdited { get; set; }

        private bool IsFontTypeEdited { get; set; }

        private bool IsForeColorEdited { get; set; }

        private bool IsMetadataChecked { get; set; }

        private bool IsRotationEdited { get; set; }

        private bool IsSaved { get; set; }

        private void LoadImage()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Loading image without cancellation functionality...");
                LoadImage(new CancellationToken());
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void LoadImage(CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Registering cancellation handlers...");
                cancellationToken.Register(LoadImageCancel);

                if (cancellationToken.IsCancellationRequested) return;
                CaptionCancellationTokenSource?.Cancel();
                CaptionCancellationTokenSource = new CancellationTokenSource();
                new Thread(LoadImageThread).Start(CaptionCancellationTokenSource.Token);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void LoadImageCancel()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Cancelling load of image of ""{Filename}""...");
                CaptionCancellationTokenSource?.Cancel();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void LoadOriginalThread(object state)
        {
            Image originalImage;

            var cancellationToken = (CancellationToken) state;

            // create dependencies
            var imageService = NinjectKernel.Get<IImageService>();
            var logService = NinjectKernel.Get<ILogService>();

            // initialise variables
            var stopwatch = Stopwatch.StartNew();

            logService.TraceEnter();
            try
            {
                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace($@"Loading original image for ""{Filename}"" from disk...");
                    using (var fileStream = new FileStream(Filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        _originalImage = System.Drawing.Image.FromStream(fileStream);
                    }

                    logService.Trace($@"Checking rotation of ""{Filename}""...");
                    switch (Rotation)
                    {
                        case Rotations.Ninety:
                            // rotate it into position
                            _originalImage.RotateFlip(RotateFlipType.Rotate90FlipNone);

                            // make a copy of it
                            originalImage = _originalImage;
                            _originalImage = new Bitmap(originalImage);
                            originalImage.Dispose();

                            break;
                        case Rotations.OneEighty:
                            // rotate it into position
                            _originalImage.RotateFlip(RotateFlipType.Rotate180FlipNone);

                            // make a copy of it
                            originalImage = _originalImage;
                            _originalImage = new Bitmap(originalImage);
                            originalImage.Dispose();

                            break;
                        case Rotations.TwoSeventy:
                            // rotate it into position
                            _originalImage.RotateFlip(RotateFlipType.Rotate270FlipNone);

                            // make a copy of it
                            originalImage = _originalImage;
                            _originalImage = new Bitmap(originalImage);
                            originalImage.Dispose();

                            break;

                    }
            }
            finally
            {
                logService.Trace($@"Flagging that original image has been loaded for ""{Filename}""...");
                _originalImageManualResetEvent.Set();

                logService.TraceExit(stopwatch);
            }
        }

        private void LoadMetadataThread(object state)
        {
            var cancellationToken = (CancellationToken)state;

            // get dependencies
            var imageMetadataService = NinjectKernel.Get<IImageMetadataService>();
            var imageService = NinjectKernel.Get<IImageService>();
            var logService = NinjectKernel.Get<ILogService>();

            // get timer
            var stopWatch = Stopwatch.StartNew();

            logService.TraceEnter();
            try
            {
                lock (_metadataLock)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    if (IsMetadataChecked)
                    {
                        logService.Trace($@"Metadata is already loaded for ""{Filename}"".  Exiting...");
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace($@"Loading metadata for ""{Filename}""...");
                    var metadata = imageMetadataService.Load(Filename);

                    if (metadata == null)
                    {
                        logService.Trace($@"Metadata does not exist for ""{Filename}"".  Loading Exif data...");
                        var exifData = imageService.GetExifData(Filename);

                        if (exifData == null)
                        {
                            logService.Trace($@"Exif data does not exist for ""{Filename}"".  Exiting...");

                            return;
                        }

                        logService.Trace($@"Updating ""{Filename}"" from Exif data...");
                        UpdateExifData(exifData);
                    }
                    else
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        logService.Trace($@"Loading properties from metadata...");
                        UpdateMetadata(metadata);

                        logService.Trace($@"Flagging that ""{Filename}"" has metadata...");
                        HasMetadata = true;
                    }

                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace($@"Flagging that metadata has been checked for ""{Filename}""...");
                    IsMetadataChecked = true;
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            finally
            {
                logService.Trace("Signalling that load has been completed...");
                _metadataManualResetEvent.Set();

                logService.TraceExit(stopWatch);
            }
        }

        public void LoadPreview(CancellationToken cancellationToken)
        {
            var logService = NinjectKernel.Get<ILogService>();
            var stopWatch = Stopwatch.StartNew();

            logService.TraceEnter();
            try
            {
                logService.Trace("Cancelling any in progress load...");
                LoadPreviewCancellationTokenSource?.Cancel();
                LoadPreviewCancellationTokenSource = new CancellationTokenSource();

                logService.Trace("Registering cancellation token...");
                cancellationToken.Register(LoadPreviewCancel);

                logService.Trace($@"Loading preview of ""{Filename}"" on background thread...");
                new Thread(LoadPreviewThread).Start(LoadPreviewCancellationTokenSource.Token);
            }
            finally
            {
                logService.TraceExit(stopWatch);
            }
        }

        private void LoadPreviewCancel()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Cancelling in progress load of preview of ""{Filename}""...");
                LoadPreviewCancellationTokenSource?.Cancel();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void LoadPreviewThread(object state)
        {
            var cancellationToken = (CancellationToken)state;

            // get dependencies
            var imageService = NinjectKernel.Get<IImageService>();
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($@"Checking if metadata for ""{Filename}"" has already been loaded...");
                if (!IsMetadataChecked)
                {
                    logService.Trace($@"Loading metadata for ""{Filename}"" on background thread...");
                    _metadataManualResetEvent.Reset();
                    new Thread(LoadMetadataThread).Start(cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($@"Loading preview image for ""{Filename}""...");
                var preview = imageService.Get(Filename, PreviewWidth, PreviewHeight);

                if (cancellationToken.IsCancellationRequested)
                {
                    preview?.Dispose();
                    return;
                }

                logService.Trace("Waiting for metadata to load...");
                _metadataManualResetEvent.WaitOne();

                logService.Trace($@"Checking if ""{Filename}"" has been saved...");
                if (IsSaved)
                {
                    logService.Trace($@"Adding saved icon to ""{Filename}"" preview...");
                    imageService.Overlay(preview, Resources.save,
                        PreviewWidth - Resources.save.Width - 8, 4);
                }
                else if (IsEdited)
                {
                    logService.Trace($@"Adding edited icon to ""{Filename}"" preview...");
                    imageService.Overlay(preview, Resources.edited,
                        PreviewWidth - Resources.edited.Width - 8, 4);
                }
                else if (HasMetadata)
                {
                    logService.Trace($@"Adding metadata icon to ""{Filename}"" preview...");
                    imageService.Overlay(preview, Resources.metadata,
                        PreviewWidth - Resources.metadata.Width - 8, 4);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    preview?.Dispose();
                    return;
                }

                UpdatePreview(preview);
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private CancellationTokenSource LoadMetadataCancellationTokenSource { get; set; }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace("Checking if running on UI thread...");
                if (Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Application.Current.Dispatcher.Invoke(new OnPropertyChangedDelegate(OnPropertyChanged),
                        propertyName);

                    return;
                }

                logService.Trace("Running event handlers...");
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private Image OriginalImage
        {
            get
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace("Wait for any background load to complete...");
                    _originalImageManualResetEvent.WaitOne();

                    lock (_originalImageLock)
                    {
                        _logService.Trace("Checking if this is the first call...");
                        if (_originalImage == null)
                        {
                            _logService.Trace("This is the first call.  Loading original image from disk...");
                            _originalImageManualResetEvent.Reset();
                            new Thread(LoadOriginalThread).Start(new CancellationToken());

                            _logService.Trace("Waiting for background load to complete...");
                            _originalImageManualResetEvent.WaitOne();
                        }

                        _logService.Trace("Returning a copy of the original image...");
                        return new Bitmap(_originalImage);
                    }
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public BitmapSource Preview => _previewWrapper?.BitmapSource ?? GetOpeningBitmapSource();

        private CancellationTokenSource LoadPreviewCancellationTokenSource { get; set; }

        private void RotateLeft()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Setting new rotation for ""{Filename}""...");
                switch (Rotation)
                {
                    case Rotations.Zero:
                        Rotation = Rotations.TwoSeventy;

                        break;
                    case Rotations.Ninety:
                        Rotation = Rotations.Zero;

                        break;
                    case Rotations.OneEighty:
                        Rotation = Rotations.Ninety;

                        break;
                    case Rotations.TwoSeventy:
                    default:
                        Rotation = Rotations.OneEighty;

                        break;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public ICommand RotateLeftCommand =>
            _rotateLeftCommand ?? (_rotateLeftCommand = new CommandHandler(RotateLeft, true));

        private void RotateRight()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Rotating to right...");
                switch (Rotation)
                {
                    case Rotations.Ninety:
                        Rotation = Rotations.OneEighty;

                        break;
                    case Rotations.OneEighty:
                        Rotation = Rotations.TwoSeventy;

                        break;
                    case Rotations.TwoSeventy:
                        Rotation = Rotations.Zero;

                        break;
                    default:
                        Rotation = Rotations.Ninety;

                        break;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public ICommand RotateRightCommand => _rotateRightCommand ?? (_rotateRightCommand = new CommandHandler(RotateRight, true));

        private Rotations Rotation {
            get => _rotation ?? Rotations.Zero;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(Rotation)} has changed...");
                    if (_rotation == value)
                    {
                        _logService.Trace($@"Value of {nameof(Rotation)} has not changed.  Exiting...");

                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(Rotation)}...");
                    _rotation = value;

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsRotationEdited = true;
                    IsEdited = true;

                    lock (_originalImageLock)
                    {
                        _logService.Trace($@"Clearing cached image for ""{Filename}""...");
                        _originalImage = null;
                    }

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        private TaskScheduler TaskScheduler { get; }

        public void UnloadImage()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Disposing of original image for ""{Filename}""...");
                _originalImage?.Dispose();
                _originalImage = null;

                _logService.Trace($@"Disposing of caption image for ""{Filename}""...");
                _imageWrapper?.Dispose();

                _logService.Trace($@"Defaulting to opening image for ""{Filename}""...");
                _imageWrapper = null;
                _imageStretch = Stretch.None;

                OnPropertyChanged(nameof(Image));
                OnPropertyChanged(nameof(ImageStretch));
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void UpdateExifData(ExifData exifData)
        {
            // create dependencies
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace("Checking if running on UI thread...");
                if (Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    logService.Trace("Not running on UI thread.  Dispatching to UI thread...");
                    Application.Current?.Dispatcher.Invoke(new UpdateExifDataDelegate(UpdateExifData), System.Windows.Threading.DispatcherPriority.Background, exifData);

                    return;
                }

                logService.Trace("Updating properties from Exif data...");
                if (!IsCaptionEdited)
                {
                    if (!string.IsNullOrWhiteSpace(exifData.Title))
                    {
                        logService.Trace($@"Setting caption of ""{Filename}"" to ""{exifData.Title}""...");
                        _caption = exifData.Title;
                    }
                    else
                    {
                        logService.Trace($@"Setting caption of ""{Filename}"" to ""{Path.GetFileNameWithoutExtension(Filename)}""...");
                        _caption = Path.GetFileNameWithoutExtension(Filename);
                    }

                    OnPropertyChanged(nameof(Caption));
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private void UpdateImage(Bitmap image)
        {
            // create dependencies
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace("Checking if running on UI thread...");
                if (Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    logService.Trace("Not running on UI thread.  Dispatching to UI thread...");
                    Application.Current?.Dispatcher.Invoke(new UpdateImageDelegate(UpdateImage), System.Windows.Threading.DispatcherPriority.Background, image);

                    return;
                }

                logService.Trace("Creating image source on UI thread...");
                _imageWrapper = new BitmapWrapper(image);
                _imageStretch = Stretch.Uniform;

                logService.Trace("Notifying updates...");
                OnPropertyChanged(nameof(Image));
                OnPropertyChanged(nameof(ImageStretch));
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private void UpdateMetadata(Metadata metadata)
        {
            var logService = NinjectKernel.Get<ILogService>();

            var stopwatch = Stopwatch.StartNew();

            logService.TraceEnter();
            try
            {
                logService.Trace("Checking if running on UI thread...");
                if (Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    logService.Trace("Not running on UI thread.  Dispatching to UI thread...");
                    Application.Current?.Dispatcher.Invoke(new UpdateMetadataDelegate(UpdateMetadata), System.Windows.Threading.DispatcherPriority.Background, metadata);

                    return;
                }

                logService.Trace("Updating properties from metadata...");
                if (!IsBackColorEdited && metadata.BackgroundColour.HasValue)
                {
                    logService.Trace($@"Setting backcolor for ""{Filename}"" from metadata...");
                    _backColor = System.Drawing.Color.FromArgb(metadata.BackgroundColour.Value).ToWindowsMediaColor();

                    OnPropertyChanged(nameof(BackColor));
                }

                if (!IsCaptionEdited)
                {
                    logService.Trace($@"Setting caption for ""{Filename}"" to ""{metadata.Caption}""...");
                    _caption = metadata.Caption;

                    OnPropertyChanged(nameof(Caption));
                }

                if (!IsCaptionAlignmentEdited && metadata.CaptionAlignment.HasValue)
                {
                    logService.Trace($@"Setting caption alignment for ""{Filename}"" from metadata...");
                    _captionAlignment = metadata.CaptionAlignment.Value;

                    OnPropertyChanged(nameof(CaptionAlignment));
                }

                if (!IsFontBoldEdited && metadata.FontBold.HasValue)
                {
                    logService.Trace($@"Setting font bold for ""{Filename}"" from metadata...");
                    _fontBold = metadata.FontBold.Value;

                    OnPropertyChanged(nameof(FontBold));
                }

                if (!IsFontNameEdited && !string.IsNullOrWhiteSpace(metadata.FontFamily))
                {
                    logService.Trace($@"Setting font name for ""{Filename}"" to ""{metadata.FontFamily}""...");
                    _fontName = metadata.FontFamily;

                    OnPropertyChanged(nameof(FontName));
                }

                if (!IsFontSizeEdited && metadata.FontSize.HasValue)
                {
                    logService.Trace($@"Setting font size for ""{Filename}"" to {metadata.FontSize.Value}...");
                    _fontSize = metadata.FontSize.Value;

                    OnPropertyChanged(nameof(FontSize));
                }

                if (!IsFontTypeEdited && !string.IsNullOrWhiteSpace(metadata.FontType))
                {
                    logService.Trace($@"Setting font type for ""{Filename}"" to ""{metadata.FontType}""...");
                    _fontType = metadata.FontType;

                    OnPropertyChanged(nameof(FontType));
                }

                if (!IsForeColorEdited && metadata.Colour.HasValue)
                {
                    logService.Trace($@"Setting fore colour for ""{Filename}"" from metadata...");
                    _foreColor = System.Drawing.Color.FromArgb(metadata.Colour.Value).ToWindowsMediaColor();

                    OnPropertyChanged(nameof(ForeColor));
                }

                if (!IsRotationEdited && metadata.Rotation.HasValue)
                {
                    logService.Trace($@"Setting rotation for ""{Filename}"" from metadata...");
                    _rotation = metadata.Rotation.Value;
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private void UpdatePreview(Bitmap image)
        {
            var logService = NinjectKernel.Get<ILogService>();

            var stopwatch = Stopwatch.StartNew();

            logService.TraceEnter();
            try
            {
                logService.Trace("Checking if running on UI thread...");
                if (Application.Current?.Dispatcher.CheckAccess() == false) {
                    logService.Trace("Not running on UI thread.  Dispatching to UI thread...");
                    Application.Current?.Dispatcher.Invoke(new UpdatePreviewDelegate(UpdatePreview), System.Windows.Threading.DispatcherPriority.ApplicationIdle, image);

                    return;
                }

                logService.Trace($@"Setting new preview for ""{Filename}""...");
                _previewWrapper = new BitmapWrapper(image);

                logService.Trace($@"Disposing preview for ""{Filename}""...");
                image.Dispose();

                OnPropertyChanged(nameof(Preview));
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            finally
            {
                logService.TraceExit();
            }
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ImageViewModel()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}