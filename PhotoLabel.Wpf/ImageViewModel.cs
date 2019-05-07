using PhotoLabel.DependencyInjection;
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
using System.Text;
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
            Filename = filename;

            // get dependencies
            _configurationService = NinjectKernel.Get<IConfigurationService>();
            _dialogService = NinjectKernel.Get<IDialogService>();
            _imageService = NinjectKernel.Get<IImageService>();
            _opacityService = NinjectKernel.Get<IOpacityService>();
            _taskScheduler = NinjectKernel.Get<SingleTaskScheduler>();
            _logService = NinjectKernel.Get<ILogService>();

            // initialise properties
            _fontType = "%";
            _imageStretch = Stretch.None;
            _metadataLock = new object();
            _metadataManualResetEvent = new ManualResetEvent(false);
            _originalImageLock = new object();
            _originalImageManualResetEvent = new ManualResetEvent(true);
            _saveFinishManualResetEvent = new ManualResetEvent(false);

            // load metadata on a low priority background thread
            _loadPreviewCancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(LoadPreviewThread, _loadPreviewCancellationTokenSource.Token,
                _loadPreviewCancellationTokenSource.Token, TaskCreationOptions.None, _taskScheduler);
        }

        public bool AppendDateTakenToCaption
        {
            get => _appendDateTakenToCaption ?? _configurationService.AppendDateTakenToCaption;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(AppendDateTakenToCaption)} has changed...");
                    if (_appendDateTakenToCaption == value)
                    {
                        _logService.Trace($"Value of {nameof(AppendDateTakenToCaption)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($@"Setting value of {nameof(AppendDateTakenToCaption)} to {value}...");
                    _appendDateTakenToCaption = value;

                    _logService.Trace(@"Saving value as default...");
                    _configurationService.AppendDateTakenToCaption = value;

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    _isAppendDateTakenToCaptionEdited = true;
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

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

        public Color BackColor
        {
            get => _backColor ?? _configurationService.BackgroundColour;
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

                    _logService.Trace(@"Saving back color as default...");
                    _configurationService.BackgroundColour = value;

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    _isBackColorEdited = true;
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BackColorOpacity));
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

        public string BackColorOpacity
        {
            get => _opacityService.GetOpacity(BackColor);
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace("Getting new background color...");
                    var backColor = _opacityService.SetOpacity(BackColor, value);

                    _logService.Trace("Checking if background opacity has changed...");
                    if (BackColor == backColor)
                    {
                        _logService.Trace("Background opacity has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace("Setting background opacity...");
                    _backColor = backColor;

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    _isBackColorEdited = true;
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BackColor));
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

        public int Brightness
        {
            get => _brightness ?? 0;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(Brightness)} has changed...");
                    if (_brightness == value)
                    {
                        _logService.Trace($"Value of {nameof(Brightness)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(Brightness)} to {value}...");
                    _brightness = value;

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    _isBrightnessEdited = true;
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

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
                    _isCaptionEdited = true;
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged();
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
                finally
                {
                    _logService.TraceExit(stopwatch);
                }
            }
        }

        private CaptionAlignments CaptionAlignment
        {
            get => _captionAlignment ?? _configurationService.CaptionAlignment;
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

                    _logService.Trace(@"Saving caption alignment as default...");
                    _configurationService.CaptionAlignment = value;

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    _isCaptionAlignmentEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

                    OnPropertyChanged(nameof(IsBottomCentreAlignment));
                    OnPropertyChanged(nameof(IsBottomLeftAlignment));
                    OnPropertyChanged(nameof(IsBottomRightAlignment));
                    OnPropertyChanged(nameof(IsMiddleLeftAlignment));
                    OnPropertyChanged(nameof(IsMiddleCentreAlignment));
                    OnPropertyChanged(nameof(IsMiddleRightAlignment));
                    OnPropertyChanged(nameof(IsTopCentreAlignment));
                    OnPropertyChanged(nameof(IsTopLeftAlignment));
                    OnPropertyChanged(nameof(IsTopRightAlignment));
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public string DateTaken { get; private set; }

        public void Delete()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Deleting metadata for ""{Filename}"" on a background thread...");
                Task.Factory.StartNew(DeleteThread, new CancellationToken(), TaskCreationOptions.None, _taskScheduler);
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

        private void DeleteThread()
        { 
            // create dependencies
            var imageService = NinjectKernel.Get<IImageService>();
            var imageMetadataService = NinjectKernel.Get<IImageMetadataService>();
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace($@"Checking if metadata exists for ""{Filename}""...");
                var metadata = imageMetadataService.Load(Filename);

                if (metadata == null)
                {
                    logService.Trace($@"""{Filename}"" does not have any metadata.  Exiting...");
                    return;
                }

                logService.Trace($@"Checking if ""{Filename}"" has been saved...");
                if (!string.IsNullOrWhiteSpace(metadata.OutputFilename))
                {
                    logService.Trace($@"Deleting ""{metadata.OutputFilename}""...");
                    try
                    {
                        File.Delete(metadata.OutputFilename);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }

                logService.Trace($@"Deleting metadata for ""{Filename}""...");
                imageMetadataService.Delete(Filename);

                logService.Trace($@"Resetting properties of ""{Filename}"" to defaults...");
                _backColor = null;
                OnPropertyChanged(nameof(BackColor));
                _captionAlignment = null;
                OnPropertyChanged(nameof(CaptionAlignment));
                _fontBold = null;
                OnPropertyChanged(nameof(FontBold));
                _fontFamily = null;
                OnPropertyChanged(nameof(FontFamily));
                _fontSize = null;
                OnPropertyChanged(nameof(FontSize));
                _fontType = null;
                OnPropertyChanged(nameof(FontType));
                _foreColor = null;
                OnPropertyChanged(nameof(ForeColor));
                _imageFormat = null;
                OnPropertyChanged(nameof(ImageFormat));
                _rotation = null;

                logService.Trace($@"Flagging that ""{Filename}"" does not have metadata...");
                HasMetadata = false;

                logService.Trace($@"Metadata does not exist for ""{Filename}"".  Loading Exif data...");
                var exifData = imageService.GetExifData(Filename);

                if (exifData != null)
                {
                    if (!string.IsNullOrWhiteSpace(exifData.Title))
                    {
                        logService.Trace($@"Setting caption of ""{Filename}"" to ""{exifData.Title}""...");
                        _caption = exifData.Title;
                    }
                    else
                    {
                        logService.Trace(
                            $@"Setting caption of ""{Filename}"" to ""{Path.GetFileNameWithoutExtension(Filename)}""...");
                        _caption = Path.GetFileNameWithoutExtension(Filename);
                    }

                    OnPropertyChanged(nameof(Caption));
                }

                logService.Trace($@"Flagging that ""{Filename}"" has not been edited...");
                IsEdited = false;
                IsSaved = false;

                logService.Trace($@"Reloading image of ""{Filename}""...");
                LoadImage();
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

        public string Filename { get; }

        private BitmapSource GetOpeningBitmapSource()
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
            finally
            {
                loggingService.TraceExit();
            }
        }

        public ImageFormat ImageFormat
        {
            get => _imageFormat ?? _configurationService.ImageFormat;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(ImageFormat)} has changed...");
                    if (_imageFormat == value)
                    {
                        _logService.Trace($"Value of {nameof(ImageFormat)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($@"Setting value of {nameof(ImageFormat)} to ""{value}""...");
                    _imageFormat = value;

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    _isImageFormatEdited = true;
                    IsEdited = true;

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

        public bool FontBold
        {
            get => _fontBold ?? _configurationService.FontBold;
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

                    _logService.Trace(@"Saving font bold as default...");
                    _configurationService.FontBold = value;

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    _isFontBoldEdited = true;
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

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

        public FontFamily FontFamily
        {
            get => _fontFamily ?? new FontFamily(_configurationService.FontName);
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(FontFamily)} has changed...");
                    if (Equals(_fontFamily, value))
                    {
                        _logService.Trace($"Value of {nameof(FontFamily)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($@"Setting value of {nameof(FontFamily)} to ""{value.Source}""...");
                    _fontFamily = value;

                    _logService.Trace($@"Saving ""{value}"" as default font...");
                    _configurationService.FontName = value.Source;

                    _logService.Trace($@"Flagging that ""{FontFamily}"" has been edited...");
                    _isFontFamilyEdited = true;
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

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

        public float FontSize
        {
            get => _fontSize ?? _configurationService.FontSize;
            set
            {
                _logService.TraceEnter();
                try
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
                    _isFontSizeEdited = true;
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

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
                    _isFontTypeEdited = true;
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

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

        public Color ForeColor
        {
            get => _foreColor ?? _configurationService.Colour;
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

                    _logService.Trace(@"Saving as default fore color...");
                    _configurationService.Colour = value;

                    _logService.Trace($@"Flagging that ""{Filename}"" has been edited...");
                    _isForeColorEdited = true;
                    IsEdited = true;

                    _logService.Trace($@"Loading image for ""{Filename}""...");
                    LoadImage();

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

        public Bitmap LoadOriginalImage()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Wait for any background load to complete...");
                _originalImageManualResetEvent.WaitOne(30000);

                lock (_originalImageLock)
                {
                    _logService.Trace("Checking if this is the first call...");
                    if (_originalImage == null)
                    {
                        _logService.Trace("This is the first call.  Loading original image from disk...");
                        _originalImageManualResetEvent.Reset();
                        new Thread(LoadOriginalThread).Start(new CancellationToken());

                        _logService.Trace("Waiting for background load to complete...");
                        _originalImageManualResetEvent.WaitOne(30000);
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

        public bool HasDateTaken => !string.IsNullOrWhiteSpace(DateTaken);

        public bool HasMetadata
        {
            get => _hasMetadata;
            private set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(HasMetadata)} has changed...");
                    if (_hasMetadata == value)
                    {
                        _logService.Trace($"Value of {nameof(HasMetadata)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(HasMetadata)} to {value}...");
                    _hasMetadata = value;

                    _logService.Trace($@"Reloading preview image for ""{Filename}""...");
                    _loadPreviewCancellationTokenSource?.Cancel();
                    _loadPreviewCancellationTokenSource = new CancellationTokenSource();
                    LoadPreview(true, _loadPreviewCancellationTokenSource.Token);

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }

            }
        }

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

        public bool IsBottomCentreAlignment
        {
            get => CaptionAlignment == CaptionAlignments.BottomCentre;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace("Checking if Bottom Centre alignment is required...");
                    if (!value)
                    {
                        _logService.Trace("Bottom Centre alignment is not required.  Exiting...");
                        return;
                    }

                    _logService.Trace("Setting caption alignment to Bottom Centre...");
                    CaptionAlignment = CaptionAlignments.BottomCentre;

                    _logService.Trace("Flagging that image has been edited...");
                    IsEdited = true;
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

        public bool IsBottomLeftAlignment
        {
            get => CaptionAlignment == CaptionAlignments.BottomLeft;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace("Checking if Bottom Left alignment is required...");
                    if (!value)
                    {
                        _logService.Trace("Bottom Left alignment is not required.  Exiting...");
                        return;
                    }

                    _logService.Trace("Setting caption alignment to Bottom Left...");
                    CaptionAlignment = CaptionAlignments.BottomLeft;

                    _logService.Trace("Flagging that image has been edited...");
                    IsEdited = true;
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

        public bool IsBottomRightAlignment
        {
            get => CaptionAlignment == CaptionAlignments.BottomRight;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace("Checking if Bottom right alignment is required...");
                    if (!value)
                    {
                        _logService.Trace("Bottom right alignment is not required.  Exiting...");
                        return;
                    }

                    _logService.Trace("Setting caption alignment to Bottom right...");
                    CaptionAlignment = CaptionAlignments.BottomRight;

                    _logService.Trace("Flagging that image has been edited...");
                    IsEdited = true;
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

        public bool IsEdited
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

                    _logService.Trace($@"Flagging that ""{Filename}"" is not saved...");
                    _isSaved = false;

                    _logService.Trace($@"Reloading preview image for ""{Filename}""...");
                    LoadPreview(true, new CancellationToken());

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public bool IsMiddleCentreAlignment
        {
            get => CaptionAlignment == CaptionAlignments.MiddleCentre;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace("Checking if Middle Centre alignment is required...");
                    if (!value)
                    {
                        _logService.Trace("Middle Centre alignment is not required.  Exiting...");
                        return;
                    }

                    _logService.Trace("Setting caption alignment to Middle Centre...");
                    CaptionAlignment = CaptionAlignments.MiddleCentre;

                    _logService.Trace("Flagging that image has been edited...");
                    IsEdited = true;
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

        public bool IsMiddleLeftAlignment
        {
            get => CaptionAlignment == CaptionAlignments.MiddleLeft;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace("Checking if Middle left alignment is required...");
                    if (!value)
                    {
                        _logService.Trace("Middle left alignment is not required.  Exiting...");
                        return;
                    }

                    _logService.Trace("Setting caption alignment to Middle left...");
                    CaptionAlignment = CaptionAlignments.MiddleLeft;

                    _logService.Trace("Flagging that image has been edited...");
                    IsEdited = true;
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

        public bool IsMiddleRightAlignment
        {
            get => CaptionAlignment == CaptionAlignments.MiddleRight;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace("Checking if Middle Right alignment is required...");
                    if (!value)
                    {
                        _logService.Trace("Middle Right alignment is not required.  Exiting...");
                        return;
                    }

                    _logService.Trace("Setting caption alignment to Middle Right...");
                    CaptionAlignment = CaptionAlignments.MiddleRight;

                    _logService.Trace("Flagging that image has been edited...");
                    IsEdited = true;
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

        private bool IsSaved
        {
            get => _isSaved;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(IsSaved)} has changed...");
                    if (_isSaved == value)
                    {
                        _logService.Trace($"Value of {nameof(IsSaved)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting value of {nameof(IsSaved)} to {value}...");
                    _isSaved = value;

                    _logService.Trace($@"Reloading preview image for ""{Filename}""...");
                    LoadPreview(true, new CancellationToken());
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public bool IsTopCentreAlignment
        {
            get => CaptionAlignment == CaptionAlignments.TopCentre;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace("Checking if top centre alignment is required...");
                    if (!value)
                    {
                        _logService.Trace("Top centre alignment is not required.  Exiting...");
                        return;
                    }

                    _logService.Trace("Setting caption alignment to top centre...");
                    CaptionAlignment = CaptionAlignments.TopCentre;

                    _logService.Trace("Flagging that image has been edited...");
                    IsEdited = true;
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

        public bool IsTopLeftAlignment
        {
            get => CaptionAlignment == CaptionAlignments.TopLeft;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace("Checking if top left alignment is required...");
                    if (!value)
                    {
                        _logService.Trace("Top left alignment is not required.  Exiting...");
                        return;
                    }

                    _logService.Trace("Setting caption alignment to top left...");
                    CaptionAlignment = CaptionAlignments.TopLeft;

                    _logService.Trace("Flagging that image has been edited...");
                    IsEdited = true;
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

        public bool IsTopRightAlignment
        {
            get => CaptionAlignment == CaptionAlignments.TopRight;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace("Checking if top right alignment is required...");
                    if (!value)
                    {
                        _logService.Trace("Top right alignment is not required.  Exiting...");
                        return;
                    }

                    _logService.Trace("Setting caption alignment to top right...");
                    CaptionAlignment = CaptionAlignments.TopRight;

                    _logService.Trace("Flagging that image has been edited...");
                    IsEdited = true;
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

        public float? Latitude { get; private set; }

        public float? Longitude { get; private set; }

        public BitmapSource Preview => _previewWrapper?.BitmapSource ?? GetOpeningBitmapSource();

        public void RotateLeft()
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
                    default:
                        Rotation = Rotations.OneEighty;

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

        public void RotateRight()
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
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private Rotations Rotation
        {
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
                    _isRotationEdited = true;
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

        private void LoadImageThread(object state)
        {
            var cancellationToken = (CancellationToken) state;

            LoadImageThread(cancellationToken);
        }

        private void LoadImageThread(CancellationToken cancellationToken)
        {
            // create dependencies
            var imageService = NinjectKernel.Get<IImageService>();
            var logService = NinjectKernel.Get<ILogService>();

            // initialise variables
            var stopwatch = Stopwatch.StartNew();

            logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                using (var originalImage = LoadOriginalImage())
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace($@"Checking if metadata for ""{Filename}"" has already been loaded...");
                    LoadMetadataThread(cancellationToken);

                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace($@"Rotating ""{Filename}""...");
                    switch (Rotation)
                    {
                        case Rotations.Ninety:
                            originalImage.RotateFlip(RotateFlipType.Rotate90FlipNone);

                            break;
                        case Rotations.OneEighty:
                            originalImage.RotateFlip(RotateFlipType.Rotate180FlipNone);

                            break;
                        case Rotations.TwoSeventy:
                            originalImage.RotateFlip(RotateFlipType.Rotate270FlipNone);

                            break;
                    }

                    // duplicate the image
                    using (var duplicateImage = new Bitmap(originalImage))
                    {
                        // get the variables
                        var brightness = Brightness;

                        if (cancellationToken.IsCancellationRequested) return;
                        logService.Trace($@"Adjusting brightness of ""{Filename}"" to {brightness}...");
                        var brightenedImage =
                            brightness == 0 ? duplicateImage : imageService.Brightness(duplicateImage, brightness);
                        try
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            logService.Trace("Building caption...");
                            var captionBuilder = new StringBuilder(Caption);
                            if (AppendDateTakenToCaption && !string.IsNullOrWhiteSpace(DateTaken))
                            {
                                if (!string.IsNullOrWhiteSpace(Caption)) captionBuilder.Append(" - ");
                                captionBuilder.Append(DateTaken);
                            }

                            if (cancellationToken.IsCancellationRequested) return;
                            var brush = new SolidBrush(ForeColor.ToDrawingColor());
                            logService.Trace($@"Captioning ""{Filename}"" with ""{Caption}""...");
                            var captionedImage = imageService.Caption(brightenedImage, captionBuilder.ToString(),
                                CaptionAlignment, FontFamily.Source, FontSize, FontType,
                                FontBold, brush, BackColor.ToDrawingColor(), cancellationToken);

                            if (cancellationToken.IsCancellationRequested) return;
                            UpdateImage(captionedImage);
                        }
                        finally
                        {
                            if (brightness != 0) brightenedImage?.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                logService.TraceExit(stopwatch);
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
                _loadImageCancellationTokenSource?.Cancel();
                _loadImageCancellationTokenSource = new CancellationTokenSource();
                new Thread(LoadImageThread).Start(_loadImageCancellationTokenSource.Token);
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

        private void LoadImageCancel()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Cancelling load of image of ""{Filename}""...");
                _loadImageCancellationTokenSource?.Cancel();
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void LoadOriginalThread(object state)
        {
            var cancellationToken = (CancellationToken)state;

            // create dependencies
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
            }
            catch (Exception ex)
            {
                OnError(ex);
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
                    if (_isMetadataChecked)
                    {
                        logService.Trace($@"Metadata is already loaded for ""{Filename}"".  Exiting...");
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace($@"Loading metadata for ""{Filename}""...");
                    var metadata = imageMetadataService.Load(Filename);

                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace($@"Flagging that metadata has been checked for ""{Filename}""...");
                    _isMetadataChecked = true;

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
                        logService.Trace(@"Loading properties from metadata...");
                        UpdateMetadata(metadata);

                        logService.Trace($@"Flagging that ""{Filename}"" has metadata...");
                        HasMetadata = true;
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                logService.Trace("Signalling that load has been completed...");
                _metadataManualResetEvent.Set();

                logService.TraceExit(stopWatch);
            }
        }

        public void LoadPreview(bool refresh, CancellationToken cancellationToken)
        {
            var logService = NinjectKernel.Get<ILogService>();
            var stopWatch = Stopwatch.StartNew();

            logService.TraceEnter();
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
                _loadPreviewCancellationTokenSource?.Cancel();
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void LoadPreviewThread(object state)
        {
            var cancellationToken = (CancellationToken) state;

            LoadPreviewThread(cancellationToken);
        }

        private void LoadPreviewThread(CancellationToken cancellationToken) {
            // get dependencies
            var imageService = NinjectKernel.Get<IImageService>();
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($@"Checking if metadata for ""{Filename}"" has already been loaded...");
                if (!_isMetadataChecked)
                {
                    logService.Trace($@"Loading metadata for ""{Filename}"" on background thread...");
                    _metadataManualResetEvent.Reset();
                    new Thread(LoadMetadataThread).Start(cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested) return;
                logService.Trace($@"Loading preview image for ""{Filename}""...");
                using (var preview = imageService.Get(Filename, PreviewWidth, PreviewHeight))
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    logService.Trace("Waiting for metadata to load...");
                    _metadataManualResetEvent.WaitOne(30000);

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
            finally
            {
                logService.TraceExit();
            }
        }

        private void OnError(Exception ex)
        {
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
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
            finally
            {
                logService.TraceExit();
            }
        }

        public void Save(string outputPath)
        {
            if (outputPath == null) throw new ArgumentNullException(nameof(outputPath));

            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Resetting save signal for ""{Filename}""...");
                _saveFinishManualResetEvent.Reset();

                _logService.Trace(@"Creating save request...");
                var metadata = new Metadata
                {
                    AppendDateTakenToCaption = AppendDateTakenToCaption,
                    Brightness = Brightness,
                    Caption = Caption,
                    CaptionAlignment = CaptionAlignment,
                    DateTaken = DateTaken,
                    FontFamily = FontFamily.Source,
                    FontSize = FontSize,
                    FontType = FontType,
                    FontBold = FontBold,
                    Colour = ForeColor.ToDrawingColor().ToArgb(),
                    BackgroundColour = BackColor.ToDrawingColor().ToArgb(),
                    ImageFormat = ImageFormat,
                    Latitude = Latitude,
                    Longitude = Longitude,
                    OutputFilename = _imageService.GetFilename(outputPath, Filename, ImageFormat),
                    Rotation = Rotation
                };

                _logService.Trace($@"Saving to ""{metadata.OutputFilename}"" on background thread...");
                Task.Factory.StartNew(SaveThread,
                    new object[] {LoadOriginalImage(), metadata, _saveFinishManualResetEvent},
                    new CancellationToken(), TaskCreationOptions.None, _taskScheduler);
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

        private void SaveThread(object state)
        {
            var stateArray = (object[])state;
            var originalImage = (Bitmap)stateArray[0];
            var metadata = (Metadata)stateArray[1];
            var saveManualResetEvent = (ManualResetEvent)stateArray[2];

            // get dependencies
            var imageMetadataService = NinjectKernel.Get<IImageMetadataService>();
            var imageService = NinjectKernel.Get<IImageService>();
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                using (originalImage)
                {
                    logService.Trace("Building caption...");
                    var captionBuilder = new StringBuilder(metadata.Caption);
                    if (metadata.AppendDateTakenToCaption.HasValue && metadata.AppendDateTakenToCaption.Value &&
                        !string.IsNullOrWhiteSpace(metadata.DateTaken))
                    {
                        if (!string.IsNullOrWhiteSpace(metadata.Caption)) captionBuilder.Append(" - ");
                        captionBuilder.Append(metadata.DateTaken);
                    }

                    logService.Trace($@"Captioning ""{Filename}"" with ""{captionBuilder}""...");
                    var backgroundColour = System.Drawing.Color.FromArgb(metadata.BackgroundColour ?? 0);
                    var brush = new SolidBrush(System.Drawing.Color.FromArgb(metadata.Colour ?? 0));
                    var captionedImage = imageService.Caption(originalImage, captionBuilder.ToString(),
                        metadata.CaptionAlignment ?? CaptionAlignments.BottomRight, metadata.FontFamily,
                        metadata.FontSize ?? 10, metadata.FontType,
                        metadata.FontBold ?? false, brush, backgroundColour, new CancellationToken());
                    try
                    {
                        logService.Trace("Saving captioned image...");
                        imageService.Save(captionedImage, metadata.OutputFilename, metadata.ImageFormat ?? ImageFormat.Jpeg);
                    }
                    finally
                    {
                        captionedImage.Dispose();
                    }
                }

                logService.Trace("Saving image metadata...");
                imageMetadataService.Save(metadata, Filename);

                logService.Trace("Flagging that image has metadata...");
                HasMetadata = true;

                logService.Trace("Flagging that image has been saved...");
                IsEdited = false;
                IsSaved = true;
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                // set the signal to continue any waiting threads
                saveManualResetEvent.Set();

                logService.TraceExit();
            }
        }

        private void SetCaption(string parameter)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Setting caption for ""{Filename}"" to ""{parameter}""...");
                Caption = parameter.Replace("__", "_");
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

        public ICommand SetCaptionCommand =>
            _setCaptionCommand ?? (_setCaptionCommand = new CommandHandler<string>(SetCaption, true));

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
            catch (Exception ex)
            {
                OnError(ex);
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
                    Application.Current?.Dispatcher.Invoke(new UpdateExifDataDelegate(UpdateExifData),
                        DispatcherPriority.Background, exifData);

                    return;
                }

                logService.Trace("Updating properties from Exif data...");
                if (!_isCaptionEdited)
                {
                    if (!string.IsNullOrWhiteSpace(exifData.Title))
                    {
                        logService.Trace($@"Setting caption of ""{Filename}"" to ""{exifData.Title}""...");
                        _caption = exifData.Title;
                    }
                    else
                    {
                        logService.Trace(
                            $@"Setting caption of ""{Filename}"" to ""{Path.GetFileNameWithoutExtension(Filename)}""...");
                        _caption = Path.GetFileNameWithoutExtension(Filename);
                    }

                    OnPropertyChanged(nameof(Caption));
                }

                DateTaken = exifData.DateTaken;
                OnPropertyChanged(nameof(DateTaken));
                OnPropertyChanged(nameof(HasDateTaken));

                Latitude = exifData.Latitude;
                OnPropertyChanged(nameof(Latitude));

                Longitude = exifData.Longitude;
                OnPropertyChanged(nameof(Longitude));
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
                    Application.Current?.Dispatcher.Invoke(new UpdateImageDelegate(UpdateImage),
                        DispatcherPriority.Background, image);

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
                OnError(ex);
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
                logService.Trace("Updating properties from metadata...");
                if (!_isAppendDateTakenToCaptionEdited && metadata.AppendDateTakenToCaption.HasValue)
                {
                    logService.Trace(
                        $@"Setting {nameof(AppendDateTakenToCaption)} to {metadata.AppendDateTakenToCaption.Value}...");
                    _appendDateTakenToCaption = metadata.AppendDateTakenToCaption.Value;

                    OnPropertyChanged(nameof(AppendDateTakenToCaption));
                }

                if (!_isBackColorEdited && metadata.BackgroundColour.HasValue)
                {
                    logService.Trace($@"Setting backcolor for ""{Filename}"" from metadata...");
                    _backColor = System.Drawing.Color.FromArgb(metadata.BackgroundColour.Value).ToWindowsMediaColor();

                    OnPropertyChanged(nameof(BackColor));
                    OnPropertyChanged(nameof(BackColorOpacity));
                }

                if (!_isBrightnessEdited)
                {
                    _logService.Trace($@"Setting brightness for ""{Filename}"" from metadata...");
                    _brightness = metadata.Brightness;

                    OnPropertyChanged(nameof(Brightness));
                }

                if (!_isCaptionEdited)
                {
                    logService.Trace($@"Setting caption for ""{Filename}"" to ""{metadata.Caption}""...");
                    _caption = metadata.Caption;

                    OnPropertyChanged(nameof(Caption));
                }

                if (!_isCaptionAlignmentEdited && metadata.CaptionAlignment.HasValue)
                {
                    logService.Trace($@"Setting caption alignment for ""{Filename}"" from metadata...");
                    _captionAlignment = metadata.CaptionAlignment.Value;

                    OnPropertyChanged(nameof(IsBottomCentreAlignment));
                    OnPropertyChanged(nameof(IsBottomLeftAlignment));
                    OnPropertyChanged(nameof(IsBottomRightAlignment));
                    OnPropertyChanged(nameof(IsMiddleLeftAlignment));
                    OnPropertyChanged(nameof(IsMiddleCentreAlignment));
                    OnPropertyChanged(nameof(IsMiddleRightAlignment));
                    OnPropertyChanged(nameof(IsTopCentreAlignment));
                    OnPropertyChanged(nameof(IsTopLeftAlignment));
                    OnPropertyChanged(nameof(IsTopRightAlignment));
                }

                DateTaken = metadata.DateTaken;
                OnPropertyChanged(nameof(DateTaken));

                if (!_isFontBoldEdited && metadata.FontBold.HasValue)
                {
                    logService.Trace($@"Setting font bold for ""{Filename}"" from metadata...");
                    _fontBold = metadata.FontBold.Value;

                    OnPropertyChanged(nameof(FontBold));
                }

                if (!_isFontFamilyEdited && !string.IsNullOrWhiteSpace(metadata.FontFamily))
                {
                    logService.Trace($@"Setting font name for ""{Filename}"" to ""{metadata.FontFamily}""...");
                    _fontFamily = new FontFamily(metadata.FontFamily);

                    OnPropertyChanged(nameof(FontFamily));
                }

                if (!_isFontSizeEdited && metadata.FontSize.HasValue)
                {
                    logService.Trace($@"Setting font size for ""{Filename}"" to {metadata.FontSize.Value}...");
                    _fontSize = metadata.FontSize.Value;

                    OnPropertyChanged(nameof(FontSize));
                }

                if (!_isFontTypeEdited && !string.IsNullOrWhiteSpace(metadata.FontType))
                {
                    logService.Trace($@"Setting font type for ""{Filename}"" to ""{metadata.FontType}""...");
                    _fontType = metadata.FontType;

                    OnPropertyChanged(nameof(FontType));
                }

                if (!_isForeColorEdited && metadata.Colour.HasValue)
                {
                    logService.Trace($@"Setting fore colour for ""{Filename}"" from metadata...");
                    _foreColor = System.Drawing.Color.FromArgb(metadata.Colour.Value).ToWindowsMediaColor();

                    OnPropertyChanged(nameof(ForeColor));
                }

                if (!_isImageFormatEdited && metadata.ImageFormat.HasValue)
                {
                    logService.Trace($@"Setting image format for ""{Filename}"" to ""{metadata.ImageFormat.Value}...");
                    _imageFormat = metadata.ImageFormat.Value;

                    OnPropertyChanged(nameof(ImageFormat));
                }

                Latitude = metadata.Latitude;
                OnPropertyChanged(nameof(Latitude));

                Longitude = metadata.Longitude;
                OnPropertyChanged(nameof(Longitude));

                if (!_isRotationEdited && metadata.Rotation.HasValue)
                {
                    logService.Trace($@"Setting rotation for ""{Filename}"" from metadata...");
                    _rotation = metadata.Rotation.Value;
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            finally
            {
                logService.TraceExit(stopwatch);
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
            finally
            {
                logService.TraceExit(stopwatch);
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

        private delegate void UpdateExifDataDelegate(ExifData exifData);

        private delegate void UpdateImageDelegate(Bitmap image);

        private delegate void UpdatePreviewDelegate(Bitmap image);

        #endregion

        #region variables

        private static BitmapSource _openingBitmapSource;

        private bool? _appendDateTakenToCaption;
        private Color? _backColor;
        private string _caption;
        private int? _brightness;
        private CaptionAlignments? _captionAlignment;
        private readonly IConfigurationService _configurationService;
        private readonly IDialogService _dialogService;
        private bool _disposedValue;
        private ImageFormat? _imageFormat;
        private bool? _fontBold;
        private FontFamily _fontFamily;
        private float? _fontSize;
        private string _fontType;
        private Color? _foreColor;
        private bool _hasMetadata;
        private BitmapWrapper _imageWrapper;
        private IImageService _imageService;
        private Stretch _imageStretch;
        private bool _isAppendDateTakenToCaptionEdited;
        private bool _isBackColorEdited;
        private bool _isBrightnessEdited;
        private bool _isCaptionAlignmentEdited;
        private bool _isCaptionEdited;
        private bool _isEdited;
        private bool _isFontBoldEdited;
        private bool _isFontFamilyEdited;
        private bool _isFontSizeEdited;
        private bool _isFontTypeEdited;
        private bool _isSaved;
        private readonly ILogService _logService;
        private CancellationTokenSource _loadImageCancellationTokenSource;
        private CancellationTokenSource _loadPreviewCancellationTokenSource;
        private readonly object _metadataLock;
        private readonly ManualResetEvent _metadataManualResetEvent;
        private readonly IOpacityService _opacityService;
        private Image _originalImage;
        private readonly object _originalImageLock;
        private readonly ManualResetEvent _originalImageManualResetEvent;
        private BitmapWrapper _previewWrapper;
        private Rotations? _rotation;
        private readonly ManualResetEvent _saveFinishManualResetEvent;
        private ICommand _setCaptionCommand;
        private readonly TaskScheduler _taskScheduler;
        private bool _isForeColorEdited;
        private bool _isImageFormatEdited;
        private bool _isMetadataChecked;
        private bool _isRotationEdited;

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

                // release disposable properties
                _imageWrapper?.Dispose();
                _previewWrapper?.Dispose();

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