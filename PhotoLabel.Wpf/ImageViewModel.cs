using PhotoLabel.Services;
using PhotoLabel.Services.Models;
using PhotoLabel.Wpf.Extensions;
using PhotoLabel.Wpf.Properties;
using Shared;
using System;
using System.ComponentModel;
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
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;

namespace PhotoLabel.Wpf
{
    public class ImageViewModel : IDisposable, INotifyPropertyChanged
    {
        public ImageViewModel(string filename)
        {
            // save dependencies
            _filename = filename;

            // get dependencies
            _dialogService = Injector.Get<IDialogService>();
            _imageMetadataService = Injector.Get<IImageMetadataService>();
            _imageService = Injector.Get<IImageService>();
            _opacityService = Injector.Get<IOpacityService>();
            _taskScheduler = Injector.Get<SingleTaskScheduler>();
            _logger = Injector.Get<ILogger>();

            // initialise properties
            _imageStretch = Stretch.None;
            _metadataLock = new object();
            _originalImageLock = new object();
            _originalImageManualResetEvent = new ManualResetEvent(true);
            _saveFinishManualResetEvent = new ManualResetEvent(false);

            // load metadata on a low priority background thread
            _loadMetadataCancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(LoadMetadataThread, _loadMetadataCancellationTokenSource.Token, _loadMetadataCancellationTokenSource.Token);
        }

        public bool? AppendDateTakenToCaption
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _appendDateTakenToCaption;
                }
            }
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    logger.Trace($"Checking if value of {nameof(AppendDateTakenToCaption)} has changed...");
                    if (_appendDateTakenToCaption == value)
                    {
                        logger.Trace($"Value of {nameof(AppendDateTakenToCaption)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($@"Setting value of {nameof(AppendDateTakenToCaption)} to {value}...");
                    _appendDateTakenToCaption = value;

                    logger.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsEdited = true;

                    logger.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                }
            }
        }

        public Color? BackColor
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _backColor;
                }
            }
            set
            {
                using (var logger = _logger.Block())
                {
                    try
                    {
                        logger.Trace($@"Loading metadata for ""{Filename}""...");
                        LoadMetadataThread(new CancellationToken());

                        logger.Trace($"Checking if value of {nameof(BackColor)} has changed...");
                        if (_backColor == value)
                        {
                            logger.Trace($"Value of {nameof(BackColor)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($@"Setting value of {nameof(BackColor)} to ""{value}""...");
                        _backColor = value;

                        logger.Trace($@"Setting value of {nameof(BackColorOpacity)}...");
                        _backColorOpacity = _opacityService.GetOpacity(value.Value);

                        logger.Trace($@"Flagging that ""{Filename}"" has been edited...");
                        IsEdited = true;

                        logger.Trace($@"Loading image for ""{Filename}""...");
                        LoadImage();

                        OnPropertyChanged();
                        OnPropertyChanged(nameof(BackColorOpacity));
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                    }
                }
            }
        }

        public string BackColorOpacity
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _backColorOpacity;
                }
            }
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    logger.Trace($"Checking if value of {nameof(BackColorOpacity)} has changed...");
                    if (_backColorOpacity == value)
                    {
                        logger.Trace($"Value of {nameof(BackColorOpacity)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($@"Setting value of {nameof(BackColorOpacity)} to {value}...");
                    _backColorOpacity = value;

                    logger.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsEdited = true;

                    logger.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                }
            }
        }

        public int? Brightness
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _brightness;
                }
            }
            set
            {
                try
                {
                    using (var logger = _logger.Block())
                    {
                        logger.Trace($@"Loading metadata for ""{Filename}""...");
                        LoadMetadataThread(new CancellationToken());

                        logger.Trace($"Checking if value of {nameof(Brightness)} has changed...");
                        if (_brightness == value)
                        {
                            logger.Trace($"Value of {nameof(Brightness)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($"Setting value of {nameof(Brightness)} to {value}...");
                        _brightness = value;

                        logger.Trace($@"Flagging that ""{Filename}"" has been edited...");
                        IsEdited = true;

                        logger.Trace($@"Loading image for ""{Filename}""...");
                        LoadImage();

                        OnPropertyChanged();
                    }
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public int? CanvasHeight
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _canvasHeight;
                }
            }
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    logger.Trace($"Checking if value of {nameof(CanvasHeight)} has changed...");
                    if (_canvasHeight == value)
                    {
                        logger.Trace($"Value of {nameof(CanvasHeight)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($@"Setting value of {nameof(CanvasHeight)} to ""{value}""...");
                    _canvasHeight = value;

                    logger.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsEdited = true;

                    logger.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                }
            }
        }

        public int? CanvasWidth
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _canvasWidth;
                }
            }
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    logger.Trace($"Checking if value of {nameof(CanvasWidth)} has changed...");
                    if (_canvasWidth == value)
                    {
                        logger.Trace($"Value of {nameof(CanvasWidth)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($@"Setting value of {nameof(CanvasWidth)} to ""{value}""...");
                    _canvasWidth = value;

                    logger.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsEdited = true;

                    logger.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                }
            }
        }

        public string Caption
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _caption;
                }
            }
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    logger.Trace($"Checking if value of {nameof(Caption)} has changed...");
                    if (_caption == value)
                    {
                        logger.Trace($"Value of {nameof(Caption)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($@"Setting value of {nameof(Caption)} to ""{value}""...");
                    _caption = value;

                    logger.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsEdited = true;

                    logger.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                }
            }
        }

        public CaptionAlignments? CaptionAlignment
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _captionAlignment;
                }
            }
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    try
                    {
                        logger.Trace($"Checking if value of {nameof(CaptionAlignment)} has changed...");
                        if (_captionAlignment == value)
                        {
                            logger.Trace($"Value of {nameof(CaptionAlignment)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($"Setting value of {nameof(CaptionAlignment)} to {value}...");
                        _captionAlignment = value;

                        logger.Trace($@"Flagging that ""{Filename}"" has been edited...");
                        IsEdited = true;

                        logger.Trace($@"Loading image for ""{Filename}""...");
                        LoadImage();
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                    }
                }
            }
        }

        public DateTime DateCreated => new FileInfo(Filename).CreationTime;

        public string DateTaken
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _dateTaken;
                }
            }
        }

        public void Delete()
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace($@"Checking if metadata exists for ""{Filename}""...");
                    var metadata = _imageMetadataService.Load(Filename);

                    if (metadata == null)
                    {
                        logger.Trace($@"""{Filename}"" does not have any metadata.  Exiting...");
                        return;
                    }

                    logger.Trace($@"Checking if ""{Filename}"" has been saved...");
                    if (!string.IsNullOrWhiteSpace(metadata.OutputFilename))
                    {
                        logger.Trace($@"Deleting ""{metadata.OutputFilename}""...");
                        try
                        {
                            File.Delete(metadata.OutputFilename);
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }

                    logger.Trace($@"Deleting metadata for ""{Filename}""...");
                    _imageMetadataService.Delete(Filename);

                    logger.Trace($@"Resetting properties of ""{Filename}"" to defaults...");
                    _backColor = null;
                    _captionAlignment = null;
                    _fontBold = null;
                    _fontFamily = null;
                    _fontSize = null;
                    _fontType = null;
                    _foreColor = null;
                    _imageFormat = null;
                    _rotation = null;

                    logger.Trace($@"Flagging that ""{Filename}"" does not have metadata...");
                    HasMetadata = false;

                    logger.Trace($@"Metadata does not exist for ""{Filename}"".  Loading Exif data...");
                    var exifData = _imageService.GetExifData(Filename);
                    UpdateExifData(exifData);
                    OnPropertyChanged(nameof(Caption));

                    logger.Trace($@"Flagging that ""{Filename}"" has not been edited...");
                    IsEdited = false;
                    IsSaved = false;
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public string Filename
        {
            get => _filename;
            set
            {
                try
                {
                    using (var logger = _logger.Block())
                    {
                        logger.Trace($"Checking if value of {nameof(Filename)} has changed...");
                        if (_filename == value)
                        {
                            logger.Trace($"Value of {nameof(Filename)} has not changed.  Exiting...");
                            return;
                        }

                        // save the original filename
                        var originalFilename = _filename;

                        logger.Trace($@"Setting value of {nameof(Filename)} to ""{value}""...");
                        _filename = value;

                        logger.Trace($@"Checking if ""{originalFilename}"" has metadata...");
                        if (_imageMetadataService.Exists(originalFilename))
                        {
                            logger.Trace($@"Renaming metadata file for ""{originalFilename}""...");
                            _imageMetadataService.Rename(originalFilename, value);
                        }

                        OnPropertyChanged();
                    }
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        private BitmapSource GetOpeningBitmapSource()
        {
            using (var loggingService = _logger.Block())
            {
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
                        return (BitmapSource)Application.Current.Dispatcher.Invoke(
                            new CreateOpeningBitmapSourceDelegate(GetOpeningBitmapSource),
                            DispatcherPriority.ApplicationIdle);
                    }

                    loggingService.Trace("Creating opening bitmap source...");
                    _openingBitmapSource = Imaging.CreateBitmapSourceFromHBitmap(Resources.opening.GetHbitmap(),
                        IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                    return _openingBitmapSource;
                }
                catch (Exception ex)
                {
                    OnError(ex);

                    return null;
                }
            }
        }

        public string GetCaption()
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($@"Getting caption for ""{Filename}""...");
                return _caption;
            }
        }

        public string GetDateTaken()
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($@"Getting date taken for ""{Filename}""...");
                return _dateTaken;
            }
        }

        public ImageFormat? ImageFormat
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _imageFormat;
                }
            }
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    logger.Trace($"Checking if value of {nameof(ImageFormat)} has changed...");
                    if (_imageFormat == value)
                    {
                        logger.Trace($"Value of {nameof(ImageFormat)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($@"Setting value of {nameof(ImageFormat)} to ""{value}""...");
                    _imageFormat = value;

                    logger.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsEdited = true;

                    OnPropertyChanged();
                }
            }
        }

        public bool? FontBold
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _fontBold;
                }
            }
            set
            {
                try
                {
                    using (var logger = _logger.Block())
                    {
                        logger.Trace($@"Loading metadata for ""{Filename}""...");
                        LoadMetadataThread(new CancellationToken());

                        logger.Trace($"Checking if value of {nameof(FontBold)} has changed...");
                        if (_fontBold == value)
                        {
                            logger.Trace($"Value of {nameof(FontBold)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($@"Setting value of {nameof(FontBold)} to ""{value}""...");
                        _fontBold = value;

                        logger.Trace($@"Flagging that ""{Filename}"" has been edited...");
                        IsEdited = true;

                        logger.Trace($@"Loading image for ""{Filename}""...");
                        LoadImage();

                        OnPropertyChanged();
                    }
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public FontFamily FontFamily
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _fontFamily;
                }
            }
            set
            {
                try
                {
                    using (var logger = _logger.Block())
                    {
                        logger.Trace($"Checking if value of {nameof(FontFamily)} has changed...");
                        if (Equals(_fontFamily, value))
                        {
                            logger.Trace($"Value of {nameof(FontFamily)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($@"Setting value of {nameof(FontFamily)} to ""{value.Source}""...");
                        _fontFamily = value;

                        logger.Trace($@"Flagging that ""{FontFamily}"" has been edited...");
                        IsEdited = true;

                        logger.Trace($@"Loading image for ""{Filename}""...");
                        LoadImage();

                        OnPropertyChanged();
                    }
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public float? FontSize
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _fontSize;
                }
            }
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    logger.Trace("Checking if value is greater than 0...");
                    if (value == null) throw new NullReferenceException();
                    if (value <= 0f) throw new ArgumentOutOfRangeException();

                    logger.Trace($"Checking if value of {nameof(FontSize)} has changed...");
                    if (_fontSize != null && Math.Abs(_fontSize.Value - value.Value) < Tolerance)
                    {
                        logger.Trace($"Value of {nameof(FontSize)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($"Setting value of {nameof(FontSize)} to {value}...");
                    _fontSize = value;

                    logger.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsEdited = true;

                    logger.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                }
            }
        }

        public string FontType
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _fontType;
                }
            }
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    if (value != "%" && value != "pts") throw new ArgumentOutOfRangeException(nameof(FontType));

                    logger.Trace($"Checking if value of {nameof(FontType)} has changed...");
                    if (_fontType == value)
                    {
                        logger.Trace($"Value of {nameof(FontType)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($@"Setting value of {nameof(FontType)} to ""{value}""...");
                    _fontType = value;

                    logger.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsEdited = true;

                    logger.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                }
            }
        }

        public Color? ForeColor
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _foreColor;
                }
            }
            set
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    logger.Trace($"Checking if value of {nameof(ForeColor)} has changed...");
                    if (_foreColor == value)
                    {
                        logger.Trace($"Value of {nameof(ForeColor)} has not changed.  Exiting...");
                        return;
                    }

                    logger.Trace($@"Setting value of {nameof(ForeColor)} to ""{value}""...");
                    _foreColor = value;

                    logger.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    IsEdited = true;

                    logger.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                }
            }
        }

        public Bitmap LoadOriginalImage()
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Wait for any background load to complete...");
                _originalImageManualResetEvent.WaitOne(30000);

                logger.Trace("Checking if the original image has already been cached...");
                lock (_originalImageLock)
                {
                    if (_originalImage != null)
                    {
                        logger.Trace(
                            "Original image has already been cached.  Returning a copy of the original image...");
                        return new Bitmap(_originalImage);
                    }

                    logger.Trace("Loading original image from disk...");
                    using (var fileStream = new FileStream(Filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        _originalImage = System.Drawing.Image.FromStream(fileStream);
                    }

                    logger.Trace("Notifying that original image has loaded...");
                    _originalImageManualResetEvent.Set();

                    logger.Trace("Returning a copy of the original image...");
                    return new Bitmap(_originalImage);
                }
            }
        }

        public bool HasDateTaken => !string.IsNullOrWhiteSpace(DateTaken);

        public bool HasMetadata
        {
            get => _hasMetadata;
            private set
            {
                using (var logger = _logger.Block())
                {
                    try
                    {
                        logger.Trace($"Checking if value of {nameof(HasMetadata)} has changed...");
                        if (_hasMetadata == value)
                        {
                            logger.Trace($"Value of {nameof(HasMetadata)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($"Setting value of {nameof(HasMetadata)} to {value}...");
                        _hasMetadata = value;

                        OnPropertyChanged();
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                    }
                }

            }
        }

        public BitmapSource Image => _imageWrapper?.BitmapSource ?? GetOpeningBitmapSource();

        public Stretch ImageStretch
        {
            get => _imageStretch;
            set
            {
                using (var logger = _logger.Block())
                {
                    try
                    {
                        logger.Trace($"Checking if value of {nameof(ImageStretch)} has changed...");
                        if (_imageStretch == value)
                        {
                            logger.Trace($"Value of {nameof(ImageStretch)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($"Setting value of {nameof(ImageStretch)} to {value}...");
                        _imageStretch = value;

                        OnPropertyChanged();
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                    }
                }
            }
        }

        public bool IsEdited
        {
            get => _isEdited;
            set
            {
                using (var logger = _logger.Block())
                {
                    try
                    {
                        logger.Trace($"Checking if value of {nameof(IsEdited)} has changed...");
                        if (_isEdited == value)
                        {
                            logger.Trace($"Value of {nameof(IsEdited)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($"Setting value of {nameof(IsEdited)} to {value}...");
                        _isEdited = value;

                        logger.Trace($@"Flagging that ""{Filename}"" is not saved...");
                        _isSaved = false;

                        logger.Trace($@"Reloading preview image for ""{Filename}""...");
                        LoadPreview(true, new CancellationToken());

                        OnPropertyChanged();
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                    }
                }
            }
        }

        private bool IsSaved
        {
            get => _isSaved;
            set
            {
                using (var logger = _logger.Block())
                {
                    try
                    {
                        logger.Trace($"Checking if value of {nameof(IsSaved)} has changed...");
                        if (_isSaved == value)
                        {
                            logger.Trace($"Value of {nameof(IsSaved)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($"Setting value of {nameof(IsSaved)} to {value}...");
                        _isSaved = value;

                        logger.Trace($@"Reloading preview image for ""{Filename}""...");
                        LoadPreview(true, new CancellationToken());
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                    }
                }
            }
        }

        public float? Latitude
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _latitude;
                }
            }
        }

        public float? Longitude
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _longitude;
                }
            }
        }

        public BitmapSource Preview => _previewWrapper?.BitmapSource ?? GetOpeningBitmapSource();

        public void RotateLeft()
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace($@"Setting new rotation for ""{Filename}""...");
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
                        default:
                            Rotation = Rotations.OneEighty;

                            break;
                    }
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public void RotateRight()
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace("Rotating to right...");
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
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        private Rotations? Rotation
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _rotation;
                }
            }
            set
            {
                using (var logger = _logger.Block())
                {
                    try
                    {
                        logger.Trace($@"Loading metadata for ""{Filename}""...");
                        LoadMetadataThread(new CancellationToken());

                        logger.Trace($"Checking if value of {nameof(Rotation)} has changed...");
                        if (_rotation == value)
                        {
                            logger.Trace($@"Value of {nameof(Rotation)} has not changed.  Exiting...");

                            return;
                        }

                        logger.Trace($"Setting value of {nameof(Rotation)}...");
                        _rotation = value;

                        logger.Trace($@"Flagging that ""{Filename}"" has been edited...");
                        IsEdited = true;

                        logger.Trace($@"Loading image for ""{Filename}""...");
                        LoadImage();

                        OnPropertyChanged();
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                    }
                }
            }
        }

        private void LoadImageThread(object state)
        {
            var cancellationToken = (CancellationToken)state;

            // create dependencies
            var imageService = Injector.Get<IImageService>();

            using (var logService = _logger.Block())
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    using (var originalImage = LoadOriginalImage())
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        logService.Trace($@"Checking if metadata for ""{Filename}"" has already been loaded...");
                        LoadMetadataThread(cancellationToken);

                        // get the variables
                        if (cancellationToken.IsCancellationRequested) return;
                        var backColor = _opacityService.SetOpacity(BackColor.Value, BackColorOpacity).ToDrawingColor();
                        var brightenedImage = _brightness.Value == 0 ? originalImage : imageService.Brightness(originalImage, _brightness.Value);
                        try
                        {
                            using (var brush = new SolidBrush(ForeColor.Value.ToDrawingColor()))
                            {
                                if (cancellationToken.IsCancellationRequested) return;
                                logService.Trace($@"Captioning ""{Filename}"" with ""{Caption}""...");
                                using (var captionedImage = imageService.Caption(brightenedImage, Caption,
                                    AppendDateTakenToCaption.Value, DateTaken, Rotation.Value,
                                    CaptionAlignment.Value, FontFamily.Source, FontSize.Value, FontType,
                                    FontBold.Value, brush, backColor, UseCanvas.Value, CanvasWidth.Value, CanvasHeight.Value, cancellationToken))
                                {
                                    if (cancellationToken.IsCancellationRequested) return;
                                    UpdateImage(captionedImage);
                                }
                            }
                        }
                        finally
                        {
                            if (_brightness.Value != 0) brightenedImage?.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        private void LoadImage()
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Loading image without cancellation functionality...");
                LoadImage(new CancellationToken());
            }
        }

        public void LoadImage(CancellationToken cancellationToken)
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace("Registering cancellation handlers...");
                    cancellationToken.Register(LoadImageCancel);

                    if (cancellationToken.IsCancellationRequested) return;
                    _loadImageCancellationTokenSource?.Cancel();
                    _loadImageCancellationTokenSource = new CancellationTokenSource();
                    new Thread(LoadImageThread).Start(_loadImageCancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        private void LoadImageCancel()
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace($@"Cancelling load of image of ""{Filename}""...");
                    _loadImageCancellationTokenSource?.Cancel();
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        public void LoadMetadata(CancellationToken cancellationToken)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Registering cancellation handlers...");
                cancellationToken.Register(LoadMetadataCancel);

                if (cancellationToken.IsCancellationRequested) return;
                _loadMetadataCancellationTokenSource?.Cancel();
                _loadMetadataCancellationTokenSource = new CancellationTokenSource();
                new Thread(LoadMetadataThread).Start(_loadMetadataCancellationTokenSource.Token);
            }
        }

        private void LoadMetadataCancel()
        {
            try
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Cancelling load of metadata for ""{Filename}""...");
                    _loadMetadataCancellationTokenSource?.Cancel();
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void LoadMetadataThread(object state)
        {
            var cancellationToken = (CancellationToken)state;

            // get dependencies
            var imageMetadataService = Injector.Get<IImageMetadataService>();
            var imageService = Injector.Get<IImageService>();

            try
            {
                using (var logger = _logger.Block())
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    logger.Trace($@"Checking if metadata is already loaded for ""{Filename}""...");
                    if (_isMetadataLoaded)
                    {
                        logger.Trace($@"Metadata is already loaded for ""{Filename}"".  Exiting...");
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested) return;
                    logger.Trace($@"Getting metadata lock for ""{Filename}""...");
                    lock (_metadataLock)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        if (_isMetadataLoaded)
                        {
                            logger.Trace($@"Metadata is already loaded for ""{Filename}"".  Exiting...");
                            return;
                        }

                        if (cancellationToken.IsCancellationRequested) return;
                        logger.Trace($@"Loading Exif data for ""{Filename}""...");
                        var exifData = imageService.GetExifData(Filename);

                        if (cancellationToken.IsCancellationRequested) return;
                        logger.Trace($@"Updating Exif data for ""{Filename}""...");
                        UpdateExifData(exifData);

                        if (cancellationToken.IsCancellationRequested) return;
                        logger.Trace($@"Loading metadata for ""{Filename}""...");
                        var metadata = imageMetadataService.Load(Filename);

                        if (cancellationToken.IsCancellationRequested) return;
                        logger.Trace($@"Flagging that metadata has been checked for ""{Filename}""...");
                        _isMetadataLoaded = true;

                        if (cancellationToken.IsCancellationRequested) return;
                        logger.Trace($@"Checking if metadata was loaded for ""{Filename}""...");
                        if (metadata != null)
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            logger.Trace(@"Loading properties from metadata...");
                            UpdateMetadata(metadata);

                            logger.Trace($@"Flagging that ""{Filename}"" has metadata...");
                            HasMetadata = true;
                        }

                        Task.Factory.StartNew(LoadPreviewThread, cancellationToken, cancellationToken, TaskCreationOptions.None, _taskScheduler);
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        public void LoadPreview(CancellationToken cancellationToken)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($@"Checking if preview has already been loaded for ""{Filename}""...");
                if (_previewWrapper != null)
                {
                    logger.Trace($@"Preview has already been loaded for ""{Filename}""...");
                    return;
                }

                logger.Trace($@"Registering cancellation requests for ""{Filename}""...");
                cancellationToken.Register(LoadPreviewCancel);

                logger.Trace("Cancelling any in progress load...");
                _loadPreviewCancellationTokenSource?.Cancel();
                _loadPreviewCancellationTokenSource = new CancellationTokenSource();

                logger.Trace($@"Loading preview of ""{Filename}"" on background thread...");
                Task.Factory.StartNew(LoadPreviewThread, _loadPreviewCancellationTokenSource.Token,
                    _loadPreviewCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }

        public void LoadPreview(bool refresh, CancellationToken cancellationToken)
        {
            using (var logService = _logger.Block())
            {
                try
                {
                    logService.Trace("Checking if preview must be refreshed...");
                    if (!refresh && _previewWrapper != null)
                    {
                        logService.Trace("Preview does not need to be refreshed.  Exiting...");
                        return;
                    }

                    logService.Trace("Registering cancellation requests...");
                    cancellationToken.Register(LoadPreviewCancel);

                    logService.Trace("Cancelling any in progress load...");
                    _loadPreviewCancellationTokenSource?.Cancel();
                    _loadPreviewCancellationTokenSource = new CancellationTokenSource();

                    logService.Trace($@"Loading preview of ""{Filename}"" on background thread...");
                    Task.Factory.StartNew(LoadPreviewThread, _loadPreviewCancellationTokenSource.Token,
                        _loadPreviewCancellationTokenSource.Token, TaskCreationOptions.None, _taskScheduler);
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        private void LoadPreviewCancel()
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace($@"Cancelling in progress load of preview of ""{Filename}""...");
                    _loadPreviewCancellationTokenSource?.Cancel();
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private void LoadPreviewThread(object state)
        {
            var cancellationToken = (CancellationToken)state;

            // get dependencies
            var imageService = Injector.Get<IImageService>();

            using (var logService = _logger.Block())
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace($@"Loading preview image for ""{Filename}""...");
                    using (var preview = imageService.Get(Filename, PreviewWidth, PreviewHeight))
                    {
                        logService.Trace($@"Checking if ""{Filename}"" has been saved...");
                        if (IsSaved)
                        {
                            logService.Trace($@"Adding saved icon to ""{Filename}"" preview...");
                            imageService.Overlay(preview, Resources.saved,
                                PreviewWidth - Resources.saved.Width - 8, 4);
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

                        if (cancellationToken.IsCancellationRequested) return;
                        UpdatePreview(preview);
                    }
                }
                catch (ThreadAbortException)
                {
                    // ignored
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        private void OnError(Exception ex)
        {
            using (var logService = _logger.Block())
            {
                try
                {
                    logService.Trace("Checking if running on UI thread...");
                    if (Application.Current?.Dispatcher.CheckAccess() == false)
                    {
                        logService.Trace("Not running on UI thread.  Dispatching to UI thread...");
                        Application.Current?.Dispatcher.Invoke(new OnErrorDelegate(OnError), DispatcherPriority.Input, ex);

                        return;
                    }

                    logService.Trace("Logging error...");
                    logService.Error(ex);

                    logService.Trace("Notifying user of error...");
                    _dialogService.Error(Resources.ErrorText);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            using (var logService = _logger.Block())
            {
                try
                {
                    logService.Trace("Checking if running on UI thread...");
                    if (Application.Current?.Dispatcher.CheckAccess() == false)
                    {
                        logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                        Application.Current.Dispatcher.Invoke(new OnPropertyChangedDelegate(OnPropertyChanged), DispatcherPriority.ApplicationIdle,
                            propertyName);

                        return;
                    }

                    logService.Trace("Running event handlers...");
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public void Save(string outputPath)
        {
            if (outputPath == null) throw new ArgumentNullException(nameof(outputPath));

            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace($@"Resetting save signal for ""{Filename}""...");
                    _saveFinishManualResetEvent.Reset();

                    logger.Trace(@"Creating save request...");
                    var metadata = new Metadata
                    {
                        AppendDateTakenToCaption = AppendDateTakenToCaption,
                        Brightness = Brightness.Value,
                        CanvasHeight = CanvasHeight.Value,
                        CanvasWidth = CanvasWidth.Value,
                        Caption = Caption,
                        CaptionAlignment = CaptionAlignment.Value,
                        DateTaken = DateTaken,
                        FontFamily = FontFamily.Source,
                        FontSize = FontSize.Value,
                        FontType = FontType,
                        FontBold = FontBold,
                        Colour = ForeColor.Value.ToDrawingColor().ToArgb(),
                        BackgroundColour = _opacityService.SetOpacity(BackColor.Value, BackColorOpacity).ToDrawingColor().ToArgb(),
                        ImageFormat = ImageFormat,
                        Latitude = Latitude,
                        Longitude = Longitude,
                        OutputFilename = _imageService.GetFilename(outputPath, Filename, ImageFormat.Value),
                        Rotation = Rotation.Value,
                        UseCanvas = UseCanvas.Value
                    };

                    logger.Trace($@"Saving to ""{metadata.OutputFilename}"" on background thread...");
                    Task.Factory.StartNew(SaveThread,
                        new object[] { metadata, _saveFinishManualResetEvent },
                        new CancellationToken(), TaskCreationOptions.None, _taskScheduler);
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        private void SaveThread(object state)
        {
            var stateArray = (object[])state;
            var metadata = (Metadata)stateArray[0];
            var saveManualResetEvent = (ManualResetEvent)stateArray[1];

            // get dependencies
            var imageMetadataService = Injector.Get<IImageMetadataService>();
            var imageService = Injector.Get<IImageService>();

            try
            {
                using (var logService = _logger.Block())
                {
                    logService.Trace($@"Loading original image for ""{Filename}""...");
                    using (var originalImage = LoadOriginalImage())
                    {
                        logService.Trace($@"Captioning ""{Filename}"" with ""{metadata.Caption}""...");
                        var backgroundColour = System.Drawing.Color.FromArgb(metadata.BackgroundColour.Value);
                        var brush = new SolidBrush(System.Drawing.Color.FromArgb(metadata.Colour ?? 0));
                        using (var captionedImage = imageService.Caption(originalImage, metadata.Caption,
                            metadata.AppendDateTakenToCaption.Value, metadata.DateTaken, metadata.Rotation.Value,
                            metadata.CaptionAlignment.Value, metadata.FontFamily,
                            metadata.FontSize.Value, metadata.FontType,
                            metadata.FontBold.Value, brush, backgroundColour, metadata.UseCanvas.Value, metadata.CanvasWidth.Value, metadata.CanvasHeight.Value, new CancellationToken()))
                        {
                            logService.Trace("Saving captioned image...");
                            imageService.Save(captionedImage, metadata.OutputFilename,
                                metadata.ImageFormat ?? Services.ImageFormat.Jpeg);
                        }

                        logService.Trace("Saving image metadata...");
                        imageMetadataService.Save(metadata, Filename);

                        logService.Trace("Flagging that image has metadata...");
                        HasMetadata = true;

                        logService.Trace("Flagging that image has been saved...");
                        IsEdited = false;
                        IsSaved = true;
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                // set the signal to continue any waiting threads
                saveManualResetEvent.Set();
            }
        }

        private void SetCaption(string parameter)
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace($@"Setting caption for ""{Filename}"" to ""{parameter}""...");
                    Caption = parameter.Replace("__", "_");
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public ICommand SetCaptionCommand =>
            _setCaptionCommand ?? (_setCaptionCommand = new CommandHandler<string>(SetCaption, true));

        public void UnloadImage()
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    lock (_originalImageLock)
                    {
                        logger.Trace($@"Disposing of original image for ""{Filename}""...");
                        _originalImage?.Dispose();
                        _originalImage = null;
                    }

                    logger.Trace($@"Disposing of caption image for ""{Filename}""...");
                    _imageWrapper?.Dispose();

                    logger.Trace($@"Defaulting to opening image for ""{Filename}""...");
                    _imageWrapper = null;
                    _imageStretch = Stretch.None;

                    OnPropertyChanged(nameof(Image));
                    OnPropertyChanged(nameof(ImageStretch));
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        private void UpdateExifData(ExifData exifData)
        {
            using (var logService = _logger.Block())
            {
                UpdateCaption(exifData);

                _dateTaken = exifData?.DateTaken;
                _latitude = exifData?.Latitude;
                _longitude = exifData?.Longitude;
            }
        }

        private void UpdateCaption(ExifData exifData)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace("Updating properties from Exif data...");
                if (!string.IsNullOrWhiteSpace(exifData?.Title))
                {
                    logger.Trace($@"Setting caption of ""{Filename}"" to ""{exifData.Title}""...");
                    _caption = exifData.Title;
                }
                else
                {
                    logger.Trace(
                        $@"Setting caption of ""{Filename}"" to ""{Path.GetFileNameWithoutExtension(Filename)}""...");
                    _caption = Path.GetFileNameWithoutExtension(Filename);
                }
            }
        }

        private void UpdateImage(Bitmap image)
        {
            using (var logService = _logger.Block())
            {
                try
                {
                    logService.Trace("Checking if running on UI thread...");
                    if (Application.Current?.Dispatcher.CheckAccess() == false)
                    {
                        logService.Trace("Not running on UI thread.  Dispatching to UI thread...");
                        Application.Current?.Dispatcher.Invoke(new UpdateImageDelegate(UpdateImage),
                            DispatcherPriority.Background, image);

                        return;
                    }

                    logService.Trace("Creating image source on UI thread...");
                    _imageWrapper?.Dispose();
                    _imageWrapper = new BitmapWrapper(image);
                    _imageStretch = Stretch.Uniform;

                    logService.Trace("Notifying updates...");
                    OnPropertyChanged(nameof(Image));
                    OnPropertyChanged(nameof(ImageStretch));
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        private void UpdateMetadata(Metadata metadata)
        {
            using (var logger = _logger.Block())
            {
                try
                {
                    logger.Trace("Updating properties from metadata...");
                    if (metadata.AppendDateTakenToCaption.HasValue)
                    {
                        logger.Trace(
                            $@"Setting {nameof(AppendDateTakenToCaption)} to {metadata.AppendDateTakenToCaption.Value}...");
                        _appendDateTakenToCaption = metadata.AppendDateTakenToCaption.Value;
                    }

                    if (metadata.BackgroundColour.HasValue)
                    {
                        logger.Trace($@"Setting backcolor for ""{Filename}"" from metadata...");
                        _backColor = System.Drawing.Color.FromArgb(metadata.BackgroundColour.Value).ToWindowsMediaColor();
                        _backColorOpacity = _opacityService.GetOpacity(System.Drawing.Color.FromArgb(metadata.BackgroundColour.Value).ToWindowsMediaColor());
                    }

                    logger.Trace($@"Setting brightness for ""{Filename}"" from metadata...");
                    _brightness = metadata.Brightness;

                    logger.Trace($@"Setting canvas height for ""{Filename}"" to {_canvasHeight}...");
                    _canvasHeight = metadata.CanvasHeight;

                    logger.Trace($@"Setting canvas Width for ""{Filename}"" to {_canvasWidth}...");
                    _canvasWidth = metadata.CanvasWidth;

                    logger.Trace($@"Setting caption for ""{Filename}"" to ""{metadata.Caption}""...");
                    _caption = metadata.Caption;

                    if (metadata.CaptionAlignment.HasValue)
                    {
                        logger.Trace($@"Setting caption alignment for ""{Filename}"" from metadata...");
                        _captionAlignment = metadata.CaptionAlignment.Value;
                    }

                    if (metadata.FontBold.HasValue)
                    {
                        logger.Trace($@"Setting font bold for ""{Filename}"" from metadata...");
                        _fontBold = metadata.FontBold.Value;
                    }

                    if (!string.IsNullOrWhiteSpace(metadata.FontFamily))
                    {
                        logger.Trace($@"Setting font name for ""{Filename}"" to ""{metadata.FontFamily}""...");
                        _fontFamily = new FontFamily(metadata.FontFamily);
                    }

                    if (metadata.FontSize.HasValue)
                    {
                        logger.Trace($@"Setting font size for ""{Filename}"" to {metadata.FontSize.Value}...");
                        _fontSize = metadata.FontSize.Value;
                    }

                    if (!string.IsNullOrWhiteSpace(metadata.FontType))
                    {
                        logger.Trace($@"Setting font type for ""{Filename}"" to ""{metadata.FontType}""...");
                        _fontType = metadata.FontType;
                    }

                    if (metadata.Colour.HasValue)
                    {
                        logger.Trace($@"Setting fore colour for ""{Filename}"" from metadata...");
                        _foreColor = System.Drawing.Color.FromArgb(metadata.Colour.Value).ToWindowsMediaColor();
                    }

                    if (metadata.ImageFormat.HasValue)
                    {
                        logger.Trace($@"Setting image format for ""{Filename}"" to ""{metadata.ImageFormat.Value}...");
                        _imageFormat = metadata.ImageFormat.Value;
                    }

                    if (metadata.Rotation.HasValue)
                    {
                        logger.Trace($@"Setting rotation for ""{Filename}"" from metadata...");
                        _rotation = metadata.Rotation.Value;
                    }

                    if (metadata.UseCanvas.HasValue)
                    {
                        logger.Trace($@"Setting {nameof(UseCanvas)} for ""{Filename}"" to {metadata.UseCanvas}...");
                        _useCanvas = metadata.UseCanvas;
                    }
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        private void UpdatePreview(Bitmap image)
        {
            using (var logService = _logger.Block())
            {
                try
                {
                    logService.Trace("Checking if running on UI thread...");
                    if (Application.Current?.Dispatcher.CheckAccess() == false)
                    {
                        logService.Trace("Not running on UI thread.  Dispatching to UI thread...");
                        Application.Current?.Dispatcher.Invoke(new UpdatePreviewDelegate(UpdatePreview),
                            DispatcherPriority.ApplicationIdle, image);

                        return;
                    }

                    logService.Trace($@"Setting new preview for ""{Filename}""...");
                    _previewWrapper = new BitmapWrapper(image);

                    OnPropertyChanged(nameof(Preview));
                }
                catch (ThreadAbortException)
                {
                    // ignored
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public bool? UseCanvas
        {
            get
            {
                using (var logger = _logger.Block())
                {
                    logger.Trace($@"Loading metadata for ""{Filename}""...");
                    LoadMetadataThread(new CancellationToken());

                    return _useCanvas;
                }
            }
            set
            {
                try
                {
                    using (var logger = _logger.Block())
                    {
                        logger.Trace($@"Loading metadata for ""{Filename}""...");
                        LoadMetadataThread(new CancellationToken());

                        logger.Trace($"Checking if value of {nameof(UseCanvas)} has changed...");
                        if (_useCanvas == value)
                        {
                            logger.Trace($"Value of {nameof(UseCanvas)} has not changed.  Exiting...");
                            return;
                        }

                        logger.Trace($@"Setting value of {nameof(UseCanvas)} to ""{value}""...");
                        _useCanvas = value;

                        logger.Trace($@"Flagging that ""{Filename}"" has been edited...");
                        IsEdited = true;

                        logger.Trace($@"Loading image for ""{Filename}""...");
                        LoadImage();

                        OnPropertyChanged();
                    }
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public void SetDefaults(bool appendDateTakenToCaption, Color backColor, int canvasHeight, int canvasWidth, CaptionAlignments captionAlignment, bool fontBold, string fontName, float fontSize, string fontType, Color foreColor, ImageFormat imageFormat, bool useCanvas)
        {
            using (var logger = _logger.Block())
            {
                logger.Trace($@"Setting default values for ""{Filename}""...");
                _appendDateTakenToCaption = _appendDateTakenToCaption ?? appendDateTakenToCaption;
                _backColor = _backColor ?? backColor;
                _backColorOpacity = _backColorOpacity ?? _opacityService.GetOpacity(backColor);
                _brightness = _brightness ?? 0;
                _canvasHeight = _canvasHeight ?? canvasHeight;
                _canvasWidth = _canvasWidth ?? canvasWidth;
                _captionAlignment = _captionAlignment ?? captionAlignment;
                _fontBold = _fontBold ?? fontBold;
                _fontFamily = _fontFamily ?? new FontFamily(fontName);
                _fontSize = _fontSize ?? fontSize;
                _fontType = _fontType ?? fontType;
                _foreColor = _foreColor ?? foreColor;
                _imageFormat = _imageFormat ?? imageFormat;
                _rotation = _rotation ?? Rotations.Zero;
                _useCanvas = _useCanvas ?? useCanvas;
            }
        }

        #region constants

        private const double Tolerance = double.Epsilon;

        private const int PreviewHeight = 128;
        private const int PreviewWidth = 128;

        #endregion

        #region delegates

        private delegate BitmapSource CreateOpeningBitmapSourceDelegate();

        private delegate void OnErrorDelegate(Exception ex);
        private delegate void OnPropertyChangedDelegate(string propertyName = "");

        private delegate void UpdateImageDelegate(Bitmap image);

        private delegate void UpdatePreviewDelegate(Bitmap image);

        #endregion

        #region variables

        private static BitmapSource _openingBitmapSource;

        private bool? _appendDateTakenToCaption;
        private Color? _backColor;
        private string _backColorOpacity;
        private int? _canvasHeight;
        private int? _canvasWidth;
        private string _caption;
        private int? _brightness;
        private CaptionAlignments? _captionAlignment;
        private string _dateTaken;
        private readonly IDialogService _dialogService;
        private bool _disposedValue;
        private string _filename;
        private bool? _fontBold;
        private FontFamily _fontFamily;
        private float? _fontSize;
        private string _fontType;
        private Color? _foreColor;
        private bool _hasMetadata;
        private ImageFormat? _imageFormat;
        private readonly IImageMetadataService _imageMetadataService;
        private readonly IImageService _imageService;
        private BitmapWrapper _imageWrapper;
        private Stretch _imageStretch;
        private bool _isEdited;
        private bool _isSaved;
        private readonly ILogger _logger;
        private float? _latitude;
        private CancellationTokenSource _loadImageCancellationTokenSource;
        private CancellationTokenSource _loadMetadataCancellationTokenSource;
        private CancellationTokenSource _loadPreviewCancellationTokenSource;
        private float? _longitude;
        private readonly object _metadataLock;
        private readonly IOpacityService _opacityService;
        private Image _originalImage;
        private readonly object _originalImageLock;
        private readonly ManualResetEvent _originalImageManualResetEvent;
        private BitmapWrapper _previewWrapper;
        private Rotations? _rotation;
        private readonly ManualResetEvent _saveFinishManualResetEvent;
        private ICommand _setCaptionCommand;
        private readonly TaskScheduler _taskScheduler;
        private bool? _useCanvas;
        private volatile bool _isMetadataLoaded;

        #endregion

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                // cancel any in progress background tasks
                LoadImageCancel();
                LoadPreviewCancel();
                LoadMetadataCancel();

                // release disposable properties
                _imageWrapper?.Dispose();
                _loadImageCancellationTokenSource?.Dispose();
                _loadMetadataCancellationTokenSource?.Dispose();
                _loadPreviewCancellationTokenSource?.Dispose();
                _originalImageManualResetEvent?.Dispose();
                _previewWrapper?.Dispose();
                _saveFinishManualResetEvent?.Dispose();

                // wait for all background saves to complete
                _saveFinishManualResetEvent.WaitOne(30000);
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

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}