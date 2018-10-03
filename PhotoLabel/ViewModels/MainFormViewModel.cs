using AutoMapper;
using PhotoLabel.Models;
using PhotoLabel.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace PhotoLabel.ViewModels
{
    public class MainFormViewModel : IObservable
    {
        #region variables
        private System.Drawing.Image _image;
        private CancellationTokenSource _imageCancellationTokenSource;
        private readonly object _imageLock = new object();
        private readonly IImageMetadataService _imageMetadataService;
        private readonly IList<ImageModel> _images = new List<ImageModel>();
        private readonly object _imagesLock = new object();
        private readonly IImageService _imageService;
        private readonly ILogService _logService;
        private readonly IList<IObserver> _observers = new List<IObserver>();
        private CancellationTokenSource _openCancellationTokenSource;
        private readonly object _openLock = new object();
        private int _position = -1;
        private readonly object _previewLock = new object();
        private Color? _secondColour;
        #endregion

        public MainFormViewModel(
            IImageMetadataService imageMetadataService,
            IImageService imageService,
            ILogService logService)
        {
            // save dependency injections
            _imageMetadataService = imageMetadataService;
            _imageService = imageService;
            _logService = logService;
        }

        private void CacheImage()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Current position is {_position + 1} of {_images.Count}");
                if (_position > _images.Count - 2)
                {
                    _logService.Trace("There is no image to cache.  Exiting...");
                    return;
                }

                // get the name of the file to be cached
                var filename = _images[_position + 1].Filename;

                _logService.Trace($"Caching \"{filename}\" on background thread...");
                Task.Factory.StartNew(() => _imageService.Get(filename), _openCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current)
                    .ContinueWith(ExceptionHandler, _openCancellationTokenSource.Token, TaskContinuationOptions.OnlyOnFaulted);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public string Caption
        {
            get => Current?.Caption ?? string.Empty;
            set
            {
                // only process changes
                if (Caption == value) return;

                // do we have an existing image?
                if (Current != null)
                {
                    // save the new value
                    Current.Caption = value;

                    // redraw the image
                    LoadImage();

                    Notify();
                }
            }
        }

        public CaptionAlignments CaptionAlignment
        {
            get => Current?.CaptionAlignment ?? DefaultCaptionAlignment;
            set
            {
                // only process changes
                if (CaptionAlignment == value) return;

                // do we have an existing image?
                if (Current != null)
                {
                    // update the value on the image
                    Current.CaptionAlignment = value;

                    // redraw the image
                    LoadImage();
                }

                // save this as the default caption alignment
                DefaultCaptionAlignment = value;

                Notify();
            }
        }

        public Color Colour
        {
            get => Current?.Colour ?? DefaultColour;
            set
            {
                // only process changes
                if (Colour == value) return;

                // save the colour to the image
                if (Current != null)
                {
                    // save the colour on the image
                    Current.Colour = value;

                    // redraw the image on a background thread
                    LoadImage();
                }

                // save the new default colour
                DefaultColour = value;

                Notify();
            }
        }

        public int Count => _images.Count;

        private ImageModel Current
        {
            get => _images.ElementAtOrDefault(_position);
        }

        private CaptionAlignments DefaultCaptionAlignment
        {
            get => Properties.Settings.Default.CaptionAlignment;
            set
            {
                // only process changes
                if (Properties.Settings.Default.CaptionAlignment == value) return;

                // save the change
                Properties.Settings.Default.CaptionAlignment = value;
                Properties.Settings.Default.Save();
            }
        }

        private Color DefaultColour
        {
            get => Properties.Settings.Default.Color;
            set
            {
                // only process changes
                if (Properties.Settings.Default.Color == value) return;

                // save the current default as the new default
                _secondColour = Properties.Settings.Default.Color;

                // save the new value
                Properties.Settings.Default.Color = value;
                Properties.Settings.Default.Save();
            }
        }

        private Font DefaultFont
        {
            get => Properties.Settings.Default.Font;
            set
            {
                // only process changes
                if (Properties.Settings.Default.Font == value) return;

                // save the new value
                Properties.Settings.Default.Font = value;
                Properties.Settings.Default.Save();
            }
        }

        private void ExceptionHandler(Task task, object state)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Bubbling error up...");
                for (var i = 0; i < _observers.Count; i++) _observers[i].OnError(task.Exception);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public string Filename => Current?.Filename;

        public Font Font
        {
            get => Current?.Font ?? DefaultFont ?? SystemFonts.DefaultFont;
            set
            {
                // only process changes
                if (Font == value) return;

                if (Current != null)
                {
                    // save the value to the image
                    Current.Font = value;

                    // redraw the image on a background thread
                    LoadImage();
                }

                // update the default font
                DefaultFont = value;

                Notify();
            }
        }

        public Image Image => _image;

        public float? Latitude => Current?.Latitude;

        private void ExifDataThread(ImageModel imageMetadata, ManualResetEvent manualResetEvent)
        {
            _logService.TraceEnter();
            try
            {
                if (!imageMetadata.ExifLoaded)
                {
                    _logService.Trace($"Loading Exif data for \"{imageMetadata.Filename}\"...");
                    var exifData = _imageService.GetExifData(imageMetadata.Filename);
                    if (exifData != null)
                    {
                        _logService.Trace($"Populating values from Exif data for \"{imageMetadata.Filename}\"...");
                        imageMetadata.Caption = exifData.DateTaken;
                        imageMetadata.Latitude = exifData.Latitude;
                        imageMetadata.Longitude = exifData.Longitude;
                    }

                    // flag that the Exif data is loaded
                    imageMetadata.ExifLoaded = true;
                }

                // flag that the Exif is loaded
                manualResetEvent.Set();
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
                lock (_imageLock)
                {
                    // cancel any in progress load
                    _imageCancellationTokenSource?.Cancel();

                    // clear the image
                    _image = null;

                    // load the image on a background thread
                    _imageCancellationTokenSource = new CancellationTokenSource();
                    Task.Factory.StartNew(() => ImageThread(Current, _imageCancellationTokenSource.Token), _imageCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current)
                        .ContinueWith(ExceptionHandler, _imageCancellationTokenSource.Token, TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ImageThread(ImageModel imageMetadata, CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;

                // load the image on another thread
                if (cancellationToken.IsCancellationRequested) return;
                var task = Task<Image>.Factory.StartNew(() => _imageService.Get(imageMetadata.Filename), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
                task.ContinueWith(ExceptionHandler, cancellationToken, TaskContinuationOptions.OnlyOnFaulted);

                // load the metadata on another thread
                var metadataResetEvent = new ManualResetEvent(false);
                Task.Factory.StartNew(() => LoadMetadata(imageMetadata, metadataResetEvent), _imageCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current)
                    .ContinueWith(ExceptionHandler, cancellationToken, TaskContinuationOptions.OnlyOnFaulted);

                // wait for the metadata to load
                if (cancellationToken.IsCancellationRequested) return;
                metadataResetEvent.WaitOne();

                // if there was no metadata file, we need to load the Exif 
                // data to get the default caption
                var exifResetEvent = new ManualResetEvent(false);
                if (imageMetadata.MetadataExists)
                {
                    // no need to load the Exif data
                    exifResetEvent.Set();
                }
                else
                {
                    Task.Factory.StartNew(() => ExifDataThread(Current, exifResetEvent), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current)
                        .ContinueWith(ExceptionHandler, cancellationToken, TaskContinuationOptions.OnlyOnFaulted);
                }

                // wait for the Exif data to load
                exifResetEvent.WaitOne();

                // get the image
                // this will wait until the thread has completed
                var image = task.Result;

                // create the caption
                if (cancellationToken.IsCancellationRequested) return;
                var captionedImage = _imageService.Caption(image, Caption, CaptionAlignment, Font, new SolidBrush(Colour), Rotation);

                // update the image in a thread safe manner
                lock (_imageLock) _image = captionedImage;

                if (cancellationToken.IsCancellationRequested) return;
                Notify();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void LoadMetadata(
            ImageModel imageMetadata,
            ManualResetEvent manualResetEvent)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Checking if metadata is loaded for \"{imageMetadata.Filename}\"...");
                if (!imageMetadata.MetadataLoaded)
                {
                    // only one thread can update the metadata at a time
                    lock (imageMetadata)
                    {
                        _logService.Trace($"Metadata is not loaded for \"{imageMetadata.Filename}\".  Loading...");
                        var metadata = _imageMetadataService.Load(imageMetadata.Filename);

                        if (metadata != null)
                        {
                            _logService.Trace($"Populating values from metadata for \"{imageMetadata.Filename}\"...");
                            Mapper.Map(metadata, imageMetadata);
                        }

                        // don't try and load the metadata again
                        imageMetadata.MetadataLoaded = true;
                    }
                }

                // flag that we have loaded
                manualResetEvent.Set();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void PreviewThread(ImageModel imageMetadata, CancellationToken cancellationToken)
        {
            Image preview;

            _logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;

                if (cancellationToken.IsCancellationRequested) return;
                var manualResetEvent = new ManualResetEvent(false);
                Task.Factory.StartNew(() => LoadMetadata(imageMetadata, manualResetEvent), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

                if (cancellationToken.IsCancellationRequested) return;
                preview = _imageService.Get(imageMetadata.Filename, 128, 128);

                // wait for the metadata to load
                manualResetEvent.WaitOne();

                if (cancellationToken.IsCancellationRequested) return;
                if (imageMetadata.Saved)
                    preview = _imageService.Overlay(preview, Properties.Resources.saved, preview.Width - Properties.Resources.saved.Width - 4, 4);
                else if (imageMetadata.MetadataExists)
                    preview = _imageService.Overlay(preview, Properties.Resources.metadata, preview.Width - Properties.Resources.saved.Width - 4, 4);

                if (cancellationToken.IsCancellationRequested) return;
                NotifyPreview(imageMetadata.Filename, preview);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public float? Longitude => Current?.Longitude;

        private ImageModel Next => _position > -1 && _position + 1 < _images.Count ? _images[_position + 1] : null;

        public void Notify()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Notifying {_observers.Count} observers...");
                for (var i = 0; i < _observers.Count; i++) _observers[i].OnUpdate(this);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void NotifyError(Exception error)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Notifying {_observers.Count} observers of error...");
                for (var i = 0; i < _observers.Count; i++) _observers[i].OnError(error);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void NotifyOpen(IList<string> filenames)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Notifying {_observers.Count} observers of open...");
                for (var i = 0; i < _observers.Count; i++) _observers[i].OnOpen(filenames);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void NotifyPreview(string filename, Image preview)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Notifying {_observers.Count} observers of new preview image for \"{filename}\"...");
                for (var i = 0; i < _observers.Count; i++) _observers[i].OnPreview(filename, preview);
            }
            finally
            {
                _logService.TraceExit();
            }
        }
        public void Open(string directory)
        {
            _logService.TraceEnter();
            try
            {
                lock (_openLock)
                {
                    _logService.Trace("Cancelling any in progress open...");
                    _openCancellationTokenSource?.Cancel();

                    _logService.Trace($"Opening \"{directory}\" on a background thread...");
                    _openCancellationTokenSource = new CancellationTokenSource();
                    Task.Factory.StartNew(() => OpenThread(directory, _openCancellationTokenSource.Token), _openCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current)
                        .ContinueWith(ExceptionHandler, _openCancellationTokenSource.Token, TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void OpenThread(string directory, CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                if (_openCancellationTokenSource.IsCancellationRequested) return;
                lock (_imagesLock)
                {
                    if (_openCancellationTokenSource.IsCancellationRequested) return;
                    _logService.Trace("Clearing current images...");
                    _images.Clear();
                    _position = -1;

                    if (_openCancellationTokenSource.IsCancellationRequested) return;
                    _logService.Trace($"Retrieving image filenames from \"{directory}\" and it's subfolders...");
                    var filenames = _imageService.Find(directory);
                    _logService.Trace($"{filenames.Count} image files found");

                    if (_openCancellationTokenSource.IsCancellationRequested) return;
                    _logService.Trace("Building images...");
                    for (var i = 0; i < filenames.Count; i++)
                    {
                        if (_openCancellationTokenSource.IsCancellationRequested) break;
                        var imageMetadata = new ImageModel
                        {
                            Filename = filenames[i]
                        };

                        if (_openCancellationTokenSource.IsCancellationRequested) break;
                        _images.Add(imageMetadata);

                        if (_openCancellationTokenSource.IsCancellationRequested) break;
                        Task.Run(() => PreviewThread(imageMetadata, _openCancellationTokenSource.Token), _openCancellationTokenSource.Token);
                    };

                    if (_openCancellationTokenSource.IsCancellationRequested) return;
                    NotifyOpen(filenames);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public string OutputPath
        {
            get => Properties.Settings.Default.OutputPath;
            set
            {
                // only process changes
                if (Properties.Settings.Default.OutputPath == value) return;

                // save the change
                Properties.Settings.Default.OutputPath = value;
                Properties.Settings.Default.Save();

                Notify();
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

                // redraw the image on a background thread
                LoadImage();

                // cache the next image on a background thread
                CacheImage();

                Notify();
            }
        }

        public Rotations Rotation
        {
            get => Current?.Rotation ?? Rotations.Zero;
            set
            {
                // only process changes
                if (Rotation == value) return;

                // save the rotation to the image
                if (Current != null)
                {
                    // update the value
                    Current.Rotation = value;

                    // redraw the image
                    LoadImage();

                    Notify();
                }
            }
        }

        public void Save(string filename)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            if (Current == null) throw new InvalidOperationException("There is no current image");

            _logService.TraceEnter();
            try
            {
                // save the image
                using (var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    lock (_image)
                    {
                        _logService.Trace($"Saving \"{Filename}\" to \"{filename}\"...");
                        _image.Save(fileStream, ImageFormat.Jpeg);
                    }
                }

                // save the metadata
                var metadata = new Services.Models.Metadata
                {
                    Caption = Caption,
                    CaptionAlignment = CaptionAlignment,
                    Colour = Colour.ToArgb(),
                    FontBold = Font.Bold,
                    FontFamily = Font.FontFamily.Name,
                    FontSize = Font.Size,
                    Latitude = Latitude,
                    Longitude = Longitude,
                    Rotation = Rotation
                };
                _imageMetadataService.Save(metadata, Filename);

                // do we need to flag it as saved?
                if (!Current.Saved)
                {
                    // flag that the current image has been saved
                    Current.Saved = true;

                    // reload the preview
                    Task.Factory.StartNew(() => PreviewThread(Current, _openCancellationTokenSource.Token), _openCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current)
                        .ContinueWith(ExceptionHandler, _openCancellationTokenSource.Token, TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Color? SecondColour => _secondColour;

        public FormWindowState WindowState
        {
            get => Properties.Settings.Default.WindowState;
            set
            {
                // only process changes
                if (Properties.Settings.Default.WindowState == value) return;

                // save the new value
                Properties.Settings.Default.WindowState = value;
                Properties.Settings.Default.Save();

                Notify();
            }
        }

        public int Zoom
        {
            get => Properties.Settings.Default.Zoom;
            set
            {
                // only process changes
                if (Properties.Settings.Default.Zoom == value) return;

                // save the new value
                Properties.Settings.Default.Zoom = value;
                Properties.Settings.Default.Save();

                Notify();
            }
        }

        #region IObservable
        public IDisposable Subscribe(IObserver observer)
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
                    observer.OnUpdate(this);
                }

                return new Unsubscriber(_observers, observer);
            }
            finally
            {
                _logService.TraceExit();
            }
        }
        #endregion
    }
}