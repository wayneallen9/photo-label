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
using SystemFonts = System.Drawing.SystemFonts;

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
        private float _fontSize;
        private string _fontType;
        private Color? _foreColor;
        private BitmapWrapper _imageWrapper;
        private CancellationTokenSource _imageCancellationTokenSource;
        private Stretch _imageStretch;
        private bool _isEdited;
        private readonly LifoTaskScheduler _lifoTaskScheduler;
        private readonly ILogService _logService;
        private Image _originalImage;
        private CancellationTokenSource _previewCancellationTokenSource;
        private BitmapWrapper _previewWrapper;
        private ICommand _rotateLeftCommand;
        private readonly IUiThrottler _uiThrottler;
        #endregion

        public ImageViewModel(string filename)
        {
            // save dependencies
            Filename = filename;

            // get dependencies
            _configurationService = NinjectKernel.Get<IConfigurationService>();
            _lifoTaskScheduler = NinjectKernel.Get<LifoTaskScheduler>();
            _logService = NinjectKernel.Get<ILogService>();
            _uiThrottler = NinjectKernel.Get<IUiThrottler>();

            // initialise public properties
            _captionAlignment = CaptionAlignments.BottomRight;
            _fontBold = false;
            _fontSize = 10;
            _fontType = "%";
            _imageStretch = Stretch.None;

            // initialise private properties
            LoadMetadataCancellationTokenSource = new CancellationTokenSource();
            Rotation = Rotations.Zero;

            // load metadata on a low priority background thread
            Task<Metadata>.Factory.StartNew(() => LoadMetadataThread(filename, LoadMetadataCancellationTokenSource.Token),
                    LoadMetadataCancellationTokenSource.Token, TaskCreationOptions.None, _lifoTaskScheduler)
                .ContinueWith(LoadMetadataSuccess, LoadMetadataCancellationTokenSource.Token, LoadMetadataCancellationTokenSource.Token,
                    TaskContinuationOptions.OnlyOnRanToCompletion, _lifoTaskScheduler);
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
                        new CreateOpeningBitmapSourceDelegate(GetOpeningBitmapSource));
                }

                loggingService.Trace("Creating opening bitmap source...");
                _openingBitmapSource = Imaging.CreateBitmapSourceFromHBitmap(Resources.opening.GetHbitmap(),
                    IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                return _openingBitmapSource;
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

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
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

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
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

        private void CaptionSuccess(Task<Bitmap> task, object state)
        {
            var cancellationToken = (CancellationToken) state;

            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace("Checking if caption was created...");
                if (cancellationToken.IsCancellationRequested)
                {
                    logService.Trace("Caption was not created.  Exiting...");
                    return;
                }

                logService.Trace("Updating image...");
                _imageWrapper = new BitmapWrapper(task.Result);
                _imageStretch = Stretch.Uniform;
                task.Result.Dispose();

                OnPropertyChanged(nameof(Image));
                OnPropertyChanged(nameof(ImageStretch));
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private static Bitmap CaptionThread(Image originalImage, string caption, Rotations rotation, CaptionAlignments captionAlignment, string fontName, float fontSize, string fontType, bool fontBold, Color foreColor, Color backColor, CancellationToken cancellationToken)
        {
            // create dependencies
            var imageService = NinjectKernel.Get<IImageService>();
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return null;
                var brush = new SolidBrush(foreColor.ToDrawingColor());

                if (cancellationToken.IsCancellationRequested) return null;
                return imageService.Caption(originalImage, caption, rotation, captionAlignment, fontName, fontSize, fontType,
                    fontBold, brush, backColor.ToDrawingColor(), cancellationToken);
            }
            finally
            {
                logService.TraceExit();
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

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
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
            get => _fontName;
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

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
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
            get => _fontSize;
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

                _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                IsEdited = true;

                _logService.Trace($@"Loading image for ""{Filename}""...");
                LoadImage();

                OnPropertyChanged();
            }
        }

        public string FontType
        {
            get => _fontType;
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

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
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

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
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

        private bool HasExifData { get; set; }

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
                    _previewCancellationTokenSource?.Cancel();
                    _previewCancellationTokenSource = new CancellationTokenSource();
                    Task<Bitmap>.Factory.StartNew(() => LoadPreviewThread(Filename, IsSaved, IsEdited, HasMetadata, _previewCancellationTokenSource.Token),
                            _previewCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                        .ContinueWith(LoadPreviewSuccess, _previewCancellationTokenSource.Token, _previewCancellationTokenSource.Token,
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning, TaskScheduler.Default);
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        private bool IsExifLoaded { get; set; }

        private bool IsMetadataChecked { get; set; }

        private bool IsSaved { get; set; }

        private void LoadExifDataCancel()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Cancelling load of Exif data for ""{Filename}""...");
                LoadExifDataCancellationTokenSource?.Cancel();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private static ExifData LoadExifDataThread(string filename, CancellationToken cancellationToken)
        {
            // get dependencies
            var imageService = NinjectKernel.Get<IImageService>();
            var logService = NinjectKernel.Get<ILogService>();

            // start stopwatch
            var stopWatch = Stopwatch.StartNew();

            logService.TraceEnter();
            try
            {
                logService.Trace($@"Loading Exif data for ""{filename}""...");
                return cancellationToken.IsCancellationRequested ? null : imageService.GetExifData(filename);
            }
            finally
            {
                logService.TraceExit(stopWatch);
            }
        }

        private void LoadExifDataSuccess(Task<ExifData> task, object state)
        {
            var cancellationToken = (CancellationToken)state;

            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                _logService.Trace($@"Checking if Exif data was loaded for ""{Filename}""...");
                if (task.Result != null)
                {
                    _logService.Trace($@"Exif data was loaded for ""{Filename}"".  Setting properties...");
                    _caption = task.Result.Title ?? Path.GetFileNameWithoutExtension(Filename);
                }
                else
                {
                    _caption = Path.GetFileNameWithoutExtension(Filename);
                }

                _uiThrottler.Queue(() => OnPropertyChanged(nameof(Caption)));
            }
            finally
            {
                _logService.TraceExit();
            }
        }

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
                _imageCancellationTokenSource?.Cancel();
                _imageCancellationTokenSource = new CancellationTokenSource();
                new Thread(LoadImageThread).Start(new object[]
                {
                    Filename, Caption, Rotation, CaptionAlignment, FontName, FontSize, FontType, FontBold,
                    ForeColor, BackColor, _imageCancellationTokenSource.Token
                });
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
                _imageCancellationTokenSource.Cancel();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void LoadImageSuccess(Task<Image> task, object state)
        {
            var cancellationToken = (CancellationToken) state;

            // get dependencies
            var imageService = NinjectKernel.Get<IImageService>();
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($@"Saving original image for ""{Filename}""...");
                _originalImage = task.Result;

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($@"Loading image with caption for ""{Filename}"" on high priority background thread...");
                CaptionCancellationTokenSource?.Cancel();
                CaptionCancellationTokenSource = new CancellationTokenSource();
                Task<Bitmap>.Factory.StartNew(
                        () => CaptionThread(task.Result, Caption, Rotation, CaptionAlignment, FontName, FontSize,
                            FontType, FontBold, ForeColor, BackColor, CaptionCancellationTokenSource.Token),
                        CaptionCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                    .ContinueWith(CaptionSuccess, CaptionCancellationTokenSource.Token,
                        CaptionCancellationTokenSource.Token,
                        TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning,
                        TaskScheduler.Default);
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private void LoadImageThread(object state)
        {
            var stateArray = (object[]) state;
            var filename = (string) stateArray[0];
            var caption = (string) stateArray[1];
            var rotation = (Rotations) stateArray[2];
            var captionAlignment = (CaptionAlignments) stateArray[3];
            var fontName = (string) stateArray[4];
            var fontSize = (float) stateArray[5];
            var fontType = (string) stateArray[6];
            var fontBold = (bool) stateArray[7];
            var foreColor = (Color) stateArray[8];
            var backColor = (Color) stateArray[9];
            var cancellationToken = (CancellationToken) stateArray[10];

            // create dependencies
            var imageService = NinjectKernel.Get<IImageService>();
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($@"Checking if original image needs to be loaded for ""{Filename}""...");
                if (_originalImage == null)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace($@"Loading original image for ""{filename}"" from disk...");
                    using (var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        _originalImage = System.Drawing.Image.FromStream(fileStream);
                    }
                }

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($@"Captioning ""{filename}""...");
                var brush = new SolidBrush(foreColor.ToDrawingColor());
                var captionedImage = imageService.Caption(_originalImage, caption, rotation, captionAlignment, fontName,
                    fontSize, fontType, fontBold, brush, backColor.ToDrawingColor(), cancellationToken);

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($@"Converting ""{filename}"" to image source...");
                _imageWrapper = new BitmapWrapper(captionedImage);
                _imageStretch = Stretch.Uniform;
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    OnPropertyChanged(nameof(Image));
                    OnPropertyChanged(nameof(ImageStretch));
                });
            }
            finally
            {
                logService.TraceExit();
            }
        }

        public void LoadMetadata(CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace(@"Registering cancellation event...");
                cancellationToken.Register(LoadExifDataCancel);
                cancellationToken.Register(LoadMetadataCancel);

                _logService.Trace($@"Checking if ""{Filename}"" has metadata...");
                if (HasMetadata)
                {
                    _logService.Trace($@"""{Filename}"" has metadata.  Exiting...");
                    return;
                }

                _logService.Trace($@"Checking if metadata has already been checked for ""{Filename}""...");
                if (IsMetadataChecked)
                {
                    _logService.Trace($@"Checking if exit data has been loaded for ""{Filename}""...");
                    if (IsExifLoaded)
                    {
                        _logService.Trace($@"Exif data has been loaded for ""{Filename}"".  Exiting...");
                        return;
                    }

                    _logService.Trace($@"Loading Exif data for ""{Filename}"" on high priority background thread...");
                    LoadExifDataCancellationTokenSource?.Cancel();
                    LoadExifDataCancellationTokenSource = new CancellationTokenSource();
                    Task<ExifData>.Factory
                        .StartNew(() => LoadExifDataThread(Filename, LoadExifDataCancellationTokenSource.Token),
                            LoadExifDataCancellationTokenSource.Token, TaskCreationOptions.LongRunning,
                            TaskScheduler.Default)
                        .ContinueWith(LoadExifDataSuccess, LoadExifDataCancellationTokenSource.Token,
                            LoadExifDataCancellationTokenSource.Token,
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning,
                            TaskScheduler.Default);
                }
                else
                { 
                    _logService.Trace($@"Metadata for ""{Filename}"" has not been checked.  Loading metadata on a high priority thread...");
                    LoadMetadataCancellationTokenSource?.Cancel();
                    LoadMetadataCancellationTokenSource = new CancellationTokenSource();
                    Task<Metadata>.Factory
                        .StartNew(() => LoadMetadataThread(Filename, LoadMetadataCancellationTokenSource.Token),
                            LoadMetadataCancellationTokenSource.Token, TaskCreationOptions.LongRunning,
                            TaskScheduler.Default)
                        .ContinueWith(LoadMetadataSuccess, LoadMetadataCancellationTokenSource.Token,
                            LoadMetadataCancellationTokenSource.Token,
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning,
                            TaskScheduler.Default);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void LoadMetadataCancel()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Cancelling metadata load for ""{Filename}""...");
                LoadMetadataCancellationTokenSource.Cancel();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void LoadMetadataSuccess(Task<Metadata> task, object state)
        {
            var cancellationToken = (CancellationToken)state;

            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($@"Flagging that metadata has been checked for ""{Filename}""...");
                IsMetadataChecked = true;

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($@"Checking if metadata exists for ""{Filename}""...");
                if (task.Result == null)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace($@"Metadata does not exist for ""{Filename}"".  Loading Exif data on background thread...");
                    Task<ExifData>.Factory.StartNew(() => LoadExifDataThread(Filename, cancellationToken),
                            cancellationToken, TaskCreationOptions.None, TaskScheduler.Current)
                        .ContinueWith(LoadExifDataSuccess, cancellationToken, cancellationToken,
                            TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current);
                }
                else
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace($@"Flagging that metadata exists for ""{Filename}""...");
                    HasMetadata = true;

                    if (cancellationToken.IsCancellationRequested) return;
                    _logService.Trace($@"Metadata exists for ""{Filename}"".  Populating values...");
                    _caption = task.Result.Caption;
                    _captionAlignment = task.Result.CaptionAlignment ?? _configurationService.CaptionAlignment;
                    _fontName = task.Result.FontFamily ??
                                _configurationService.FontName ?? SystemFonts.DefaultFont.Name;
                    _fontSize = task.Result.FontSize ?? _configurationService.FontSize;
                    _fontType = task.Result.FontType ?? _configurationService.FontType ?? "%";
                    if (task.Result.Colour.HasValue) _foreColor = System.Drawing.Color.FromArgb(task.Result.Colour.Value).ToWindowsMediaColor();
                    if (task.Result.BackgroundColour.HasValue)
                        _backColor = System.Drawing.Color.FromArgb(task.Result.BackgroundColour.Value).ToWindowsMediaColor();
                    Rotation = task.Result.Rotation ?? Rotations.Zero;

                    _uiThrottler.Queue(() =>
                    {
                        OnPropertyChanged(nameof(BackColor));
                        OnPropertyChanged(nameof(Caption));
                        OnPropertyChanged(nameof(CaptionAlignment));
                        OnPropertyChanged(nameof(FontName));
                        OnPropertyChanged(nameof(FontSize));
                        OnPropertyChanged(nameof(FontType));
                        OnPropertyChanged(nameof(ForeColor));
                    });
                }

                if (cancellationToken.IsCancellationRequested) return;
                _logService.Trace($@"Loading preview of ""{Filename}"" on low priority background thread...");
                _previewCancellationTokenSource?.Cancel();
                _previewCancellationTokenSource = new CancellationTokenSource();
                Task<Bitmap>.Factory
                    .StartNew(
                        () => LoadPreviewThread(Filename, IsSaved, IsEdited, HasMetadata,
                            _previewCancellationTokenSource.Token), _previewCancellationTokenSource.Token, TaskCreationOptions.None, _lifoTaskScheduler)
                    .ContinueWith(LoadPreviewSuccess, _previewCancellationTokenSource.Token, _previewCancellationTokenSource.Token,
                        TaskContinuationOptions.OnlyOnRanToCompletion, _lifoTaskScheduler);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private static Metadata LoadMetadataThread(string filename, CancellationToken cancellationToken)
        {
            // get dependencies
            var imageMetadataService = NinjectKernel.Get<IImageMetadataService>();
            var logService = NinjectKernel.Get<ILogService>();

            // get timer
            var stopWatch = Stopwatch.StartNew();

            logService.TraceEnter();
            try
            {
                Thread.Sleep(100);
                logService.Trace($@"Loading metadata for ""{filename}""...");
                return cancellationToken.IsCancellationRequested ? null : imageMetadataService.Load(filename);
            }
            finally
            {
                logService.TraceExit(stopWatch);
            }
        }

        public void LoadPreview()
        {
            var logService = NinjectKernel.Get<ILogService>();
            var stopWatch = Stopwatch.StartNew();

            logService.TraceEnter();
            try
            {
                logService.Trace($@"Checking if metadata needs to be loaded for ""{Filename}""...");
                if (IsMetadataChecked)
                {
                    logService.Trace($@"Metadata has already been loaded for ""{Filename}"".  Loading preview on high priority background thread...");
                    _previewCancellationTokenSource?.Cancel();
                    _previewCancellationTokenSource = new CancellationTokenSource();
                    Task<Bitmap>.Factory.StartNew(
                            () => LoadPreviewThread(Filename, IsSaved, IsEdited, HasMetadata,
                                _previewCancellationTokenSource.Token),
                            _previewCancellationTokenSource.Token, TaskCreationOptions.LongRunning,
                            _lifoTaskScheduler)
                        .ContinueWith(LoadPreviewSuccess, _previewCancellationTokenSource.Token, _previewCancellationTokenSource.Token,
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning, _lifoTaskScheduler);
                }
                else
                {
                    logService.Trace($@"Loading metadata for ""{Filename}"" on high priority background thread...");
                    LoadMetadataCancellationTokenSource?.Cancel();
                    LoadMetadataCancellationTokenSource = new CancellationTokenSource();
                    Task<Metadata>.Factory
                        .StartNew(() => LoadMetadataThread(Filename, LoadMetadataCancellationTokenSource.Token),
                            LoadMetadataCancellationTokenSource.Token, TaskCreationOptions.LongRunning,
                            _lifoTaskScheduler)
                        .ContinueWith(LoadMetadataSuccess, LoadMetadataCancellationTokenSource.Token, LoadMetadataCancellationTokenSource.Token,
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning, _lifoTaskScheduler);
                }
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
                _previewCancellationTokenSource.Cancel();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void LoadPreviewSuccess(Task<Bitmap> task, object state)
        {
            var cancellationToken = (CancellationToken) state;

            _logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                _logService.Trace($@"Converting preview image for ""{Filename}"" to a bitmap source...");
                _previewWrapper = new BitmapWrapper(task.Result);
                task.Result.Dispose();

                // queue up the change notification
                _uiThrottler.Queue(() => OnPropertyChanged(nameof(Preview)));
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private static Bitmap LoadPreviewThread(string filename, bool isSaved, bool isEdited, bool hasMetadata, CancellationToken cancellationToken)
        {
            // get dependencies
            var imageService = NinjectKernel.Get<IImageService>();
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return null;
                logService.Trace($@"Loading preview image for ""{filename}""...");
                var preview = imageService.Get(filename, PreviewWidth, PreviewHeight);

                if (cancellationToken.IsCancellationRequested) return null;
                logService.Trace($@"Checking if ""{filename}"" has been saved...");
                if (isSaved)
                {
                    if (cancellationToken.IsCancellationRequested) return null;
                    logService.Trace($@"Adding saved icon to ""{filename}"" preview...");
                    imageService.Overlay(preview, Resources.save,
                        PreviewWidth - Resources.save.Width - 8, 4);
                } else if (isEdited)
                {
                    if (cancellationToken.IsCancellationRequested) return null;
                    logService.Trace($@"Adding edited icon to ""{filename}"" preview...");
                    imageService.Overlay(preview, Resources.edited,
                        PreviewWidth - Resources.edited.Width - 8, 4);
                }
                else if (hasMetadata)
                {
                    if (cancellationToken.IsCancellationRequested) return null;
                    logService.Trace($@"Adding metadata icon to ""{filename}"" preview...");
                    imageService.Overlay(preview, Resources.metadata,
                        PreviewWidth - Resources.metadata.Width - 8, 4);
                }

                return cancellationToken.IsCancellationRequested ? null : preview;
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private CancellationTokenSource LoadExifDataCancellationTokenSource { get; set; }

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
            finally
            {
                logService.TraceExit();
            }
        }

        public BitmapSource Preview => _previewWrapper?.BitmapSource ?? GetOpeningBitmapSource();

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

                _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                IsEdited = true;

                _logService.Trace($@"Loading image for ""{Filename}""...");
                LoadImage();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private Rotations Rotation { get; set; }

        public ICommand RotateLeftCommand =>
            _rotateLeftCommand ?? (_rotateLeftCommand = new CommandHandler(RotateLeft, true));

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