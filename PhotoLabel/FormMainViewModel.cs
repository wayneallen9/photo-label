using AutoMapper;
using PhotoLabel.Models;
using PhotoLabel.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.CompilerServices;

namespace PhotoLabel
{
    public class FormMainViewModel : INotifyPropertyChanged
    {
        #region delegates
        private delegate void OnErrorDelegate(Exception ex);
        private delegate void OnPreviewLoadedDelegate(string filename, Image image);
        private delegate void OnPropertyChangedDelegate(string propertyName);
        public delegate void PreviewLoadedEventHandler(object sender, PreviewLoadedEventArgs e);
        #endregion

        #region events
        public event ErrorEventHandler Error;
        public event PreviewLoadedEventHandler PreviewLoaded;
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region variables
        private readonly IConfigurationService _configurationService;
        private ImageModel _current;
        private List<DirectoryModel> _recentlyUsedDirectories;
        private CancellationTokenSource _imageCancellationTokenSource;
        private readonly object _imageLock = new object();
        private readonly IImageMetadataService _imageMetadataService;
        private readonly ManualResetEvent _imageManualResetEvent;
        private readonly IList<ImageModel> _images = new List<ImageModel>();
        private readonly object _imagesLock = new object();
        private readonly IImageService _imageService;
        private readonly ILogService _logService;
        private readonly IList<ViewModels.IObserver> _observers = new List<ViewModels.IObserver>();
        private CancellationTokenSource _openCancellationTokenSource;
        private readonly object _openLock = new object();
        private int _position = -1;
        private readonly object _previewLock = new object();
        private readonly IRecentlyUsedFoldersService _recentlyUsedDirectoriesService;
        private Image _secondColourImage;
        #endregion

        public FormMainViewModel(
            IConfigurationService configurationService,
            IImageMetadataService imageMetadataService,
            IImageService imageService,
            ILogService logService,
            IRecentlyUsedFoldersService recentlyUsedDirectoriesService)
        {
            // save dependency injections
            _configurationService = configurationService;
            _imageMetadataService = imageMetadataService;
            _imageService = imageService;
            _logService = logService;
            _recentlyUsedDirectoriesService = recentlyUsedDirectoriesService;

            // initialise variables
            _imageManualResetEvent = new ManualResetEvent(true);
            _recentlyUsedDirectories = Mapper.Map<List<DirectoryModel>>(_recentlyUsedDirectoriesService.Load());

            // load the second colour image
            if (_configurationService.SecondColour != null)
                _secondColourImage = _imageService.Circle(_configurationService.SecondColour.Value, 16, 16);
        }

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
                Task.Factory.StartNew(() => _imageService.Get(filename), _openCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current)
                    .ContinueWith(ExceptionHandler, _openCancellationTokenSource.Token, TaskContinuationOptions.OnlyOnFaulted);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public bool CanDelete => _current?.MetadataExists ?? false;

        public string Caption
        {
            get => _current?.Caption ?? string.Empty;
            set
            {
                // only process changes
                if (Caption == value) return;

                // do we have an existing image?
                if (_current != null)
                {
                    // save the new value
                    _current.Caption = value;

                    // redraw the image
                    LoadImage(_position);

                    OnPropertyChanged();
                }
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
                if (Colour == value) return;

                // save the current colour as the secondary colour
                _configurationService.SecondColour = Colour;
                _secondColourImage = _imageService.Circle(Colour, 16, 16);

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

        public IList<DirectoryModel> RecentlyUsedDirectories => _recentlyUsedDirectories;

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
                _current.ExifLoaded = false;
                _current.MetadataExists = false;
                _current.MetadataLoaded = false;
                _current.Rotation = Rotations.Zero;
                _current.Saved = false;

                _logService.Trace("Reloading image...");
                LoadImage(_position);

                _logService.Trace("Reloading preview...");
                LoadPreview(_current.Filename);

                _logService.Trace("Checking if there is a current output path...");
                if (string.IsNullOrWhiteSpace(OutputPath))
                {
                    _logService.Trace("There is no current output path.  Returning...");
                    return true;
                }

                _logService.Trace($"Getting output filename for \"{_current.Filename}\"...");
                var outputPath = Path.Combine(OutputPath, Path.GetFileNameWithoutExtension(Filename) + ".jpg");

                _logService.Trace($"Checking that \"{outputPath}\" is not \"{_current.Filename}\"...");
                if (outputPath == _current.Filename)
                {
                    _logService.Trace($"\"{outputPath}\" is \"{_current.Filename}\".  Returning...");
                    return true;
                }

                _logService.Trace($"Checking if \"{outputPath}\" exists...");
                if (!File.Exists(outputPath))
                {
                    _logService.Trace($"\"{outputPath}\" does not exist.  Returning...");
                    return true;
                }

                _logService.Trace($"Deleting \"{outputPath}\"...");
                File.Delete(outputPath);

                return true;
            }
            finally
            {
                _logService.TraceExit();
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

        public string Filename => _current?.Filename;

        public IList<string> Filenames => _images.Select(i => i.Filename).ToList();

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
                // only process changes
                if (FontSize == value) return;

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

        public Image Image { get; private set; }

        public IInvoker Invoker { get; set; }

        public float? Latitude => _current?.Latitude;

        private void ExifThread(ImageModel imageModel, ManualResetEvent manualResetEvent)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Checking if Exif data is already loaded for \"{imageModel.Filename}\"...");
                if (!imageModel.ExifLoaded)
                {
                    _logService.Trace($"Loading Exif data for \"{imageModel.Filename}\"...");
                    var exifData = _imageService.GetExifData(imageModel.Filename);
                    if (exifData != null)
                    {
                        _logService.Trace($"Populating values from Exif data for \"{imageModel.Filename}\"...");
                        _logService.Trace($"Date taken for \"{imageModel.Filename}\" is \"{exifData.DateTaken}\"");
                        imageModel.Caption = exifData.DateTaken;
                        imageModel.Latitude = exifData.Latitude;
                        imageModel.Longitude = exifData.Longitude;
                    }

                    // flag that the Exif data is loaded
                    imageModel.ExifLoaded = true;
                }
                else
                    _logService.Trace($"Exif data is already loaded for \"{imageModel.Filename}\".  Caption is \"{imageModel.Caption}\"");

                // flag that the Exif is loaded
                manualResetEvent.Set();
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

                    // flag that the image is loading
                    _imageManualResetEvent.Reset();

                    // clear the image
                    Image = null;

                    // get the image to load
                    var imageToLoad = _images[position];

                    // load the image on a background thread
                    _imageCancellationTokenSource = new CancellationTokenSource();
                    Task.Delay(300, _imageCancellationTokenSource.Token)
                        .ContinueWith((t, o) => ImageThread(imageToLoad, _imageCancellationTokenSource.Token), null, _imageCancellationTokenSource.Token, TaskContinuationOptions.LongRunning, TaskScheduler.Current)
                        .ContinueWith(ExceptionHandler, _imageCancellationTokenSource.Token, TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void ImageThread(ImageModel imageModel, CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                if (cancellationToken.IsCancellationRequested) return;

                // load the image on another thread
                if (cancellationToken.IsCancellationRequested) return;
                var task = Task<Image>.Factory.StartNew(() => _imageService.Get(imageModel.Filename), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
                task.ContinueWith(ExceptionHandler, cancellationToken, TaskContinuationOptions.OnlyOnFaulted);

                // load the metadata on another thread
                var metadataResetEvent = new ManualResetEvent(false);
                Task.Factory.StartNew(() => MetadataThread(imageModel, metadataResetEvent), _imageCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current)
                    .ContinueWith(ExceptionHandler, cancellationToken, TaskContinuationOptions.OnlyOnFaulted);

                // wait for the metadata to load
                if (cancellationToken.IsCancellationRequested) return;
                metadataResetEvent.WaitOne();

                // if there was no metadata file, we need to load the Exif 
                // data to get the default caption
                var exifResetEvent = new ManualResetEvent(false);
                if (imageModel.MetadataExists)
                {
                    // no need to load the Exif data
                    exifResetEvent.Set();
                }
                else
                {
                    Task.Factory.StartNew(() => ExifThread(imageModel, exifResetEvent), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current)
                        .ContinueWith(ExceptionHandler, cancellationToken, TaskContinuationOptions.OnlyOnFaulted);
                }

                // wait for the Exif data to load
                exifResetEvent.WaitOne();

                // get the image
                // this will wait until the thread has completed
                var image = task.Result;

                // work out the values to use
                var captionAlignment = imageModel.CaptionAlignment ?? _configurationService.CaptionAlignment;
                var colour = imageModel.Colour ?? _configurationService.Colour;
                var fontBold = imageModel.FontBold ?? _configurationService.FontBold;
                var fontName = imageModel.FontName ?? _configurationService.FontName;
                var fontSize = imageModel.FontSize ?? _configurationService.FontSize;
                var fontType = imageModel.FontType ?? _configurationService.FontType;
                var rotation = imageModel.Rotation ?? Rotations.Zero;

                // create the caption
                if (cancellationToken.IsCancellationRequested) return;
                _logService.Trace($"Caption for \"{imageModel.Filename}\" is \"{imageModel.Caption}\".  Creating image...");
                var captionedImage = _imageService.Caption(image, imageModel.Caption, captionAlignment, fontName, fontSize, fontType, fontBold, new SolidBrush(colour), rotation);
                try
                {
                    // update the image in a thread safe manner
                    if (cancellationToken.IsCancellationRequested) return;
                    lock (_imageLock)
                    {
                        if (cancellationToken.IsCancellationRequested) return;

                        _logService.Trace($"Setting \"{imageModel.Filename}\" as current image...");
                        _current = imageModel;

                        _logService.Trace($"Setting image for \"{imageModel.Filename}\"...");
                        Image = captionedImage;
                    }
                }
                finally
                {
                    if (cancellationToken.IsCancellationRequested) captionedImage.Dispose();
                }

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

        private void MetadataThread(
            ImageModel imageMetadata,
            ManualResetEvent manualResetEvent)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Checking if metadata is loaded for \"{imageMetadata.Filename}\"...");
                if (!imageMetadata.MetadataLoaded)
                {
                    // only one thread can load the metadata
                    lock (imageMetadata)
                    {
                        // once we have the lock, make sure another thread didn't load
                        // the metadata in the interim
                        if (!imageMetadata.MetadataLoaded)
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
                }

                // flag that we have loaded
                manualResetEvent.Set();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void LoadPreview(string filename)
        {
            _logService.TraceEnter();
            try
            {
                // find the image being previewed
                _logService.Trace($"Finding image \"{filename}\"...");
                var imageModel = _images.FirstOrDefault(i => i.Filename == filename);
                if (imageModel == null)
                {
                    _logService.Trace($"Image \"{filename}\" does not exist.  Exiting...");
                    return;
                }

                _logService.Trace($"Loading preview for \"{filename}\" on a background thread...");
                Task.Factory.StartNew(() => PreviewThread(imageModel, _openCancellationTokenSource.Token), _openCancellationTokenSource.Token)
                    .ContinueWith(ExceptionHandler, _openCancellationTokenSource.Token, TaskContinuationOptions.OnlyOnFaulted);
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
                Task.Factory.StartNew(() => MetadataThread(imageMetadata, manualResetEvent), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

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
                OnPreviewLoaded(imageMetadata.Filename, preview);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public float? Longitude => _current?.Longitude;

        private void OnError(Exception ex)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if running on UI thread...");
                if (Invoker.InvokeRequired)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoker.Invoke(new OnErrorDelegate(OnError), ex);

                    return;
                }

                _logService.Trace($"Notifying error...");
                Error?.Invoke(this, new ErrorEventArgs(ex));
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
                if (Invoker.InvokeRequired)
                {
                    _logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoker.Invoke(new OnPreviewLoadedDelegate(OnPreviewLoaded), filename, image);

                    return;
                }

                _logService.Trace($"Notifying preview loaded for {filename}...");
                PreviewLoaded?.Invoke(this, new PreviewLoadedEventArgs { Filename = filename, Image=image });
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
                if (Invoker.InvokeRequired)
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

        public void Open(string directory)
        {
            _logService.TraceEnter();
            try
            {
                lock (_openLock)
                {
                    _logService.Trace("Cancelling any in progress open...");
                    _openCancellationTokenSource?.Cancel();

                    _logService.Trace("Clearing current image...");
                    lock (_imageLock) Image = null;

                    _logService.Trace($"Opening \"{directory}\" on a background thread...");
                    _openCancellationTokenSource = new CancellationTokenSource();
                    Task.Factory.StartNew(() => OpenThread(directory, _openCancellationTokenSource.Token), _openCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current)
                        .ContinueWith(ExceptionHandler, _openCancellationTokenSource.Token, TaskContinuationOptions.OnlyOnFaulted);

                    OnPropertyChanged(nameof(Image));
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

                    // only add this directory to the recently used directories list
                    // if it contains images
                    if (filenames.Count > 0)
                    {
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
                        };

                        if (_openCancellationTokenSource.IsCancellationRequested) return;
                        _logService.Trace("Mapping to service layer...");
                        var servicesRecentlyUsedDirectories = Mapper.Map<List<Services.Models.FolderModel>>(_recentlyUsedDirectories);

                        if (_openCancellationTokenSource.IsCancellationRequested) return;
                        _logService.Trace($"Adding \"{directory}\" to list of recently used directories...");
                        servicesRecentlyUsedDirectories = _recentlyUsedDirectoriesService.Add(directory, servicesRecentlyUsedDirectories);

                        if (_openCancellationTokenSource.IsCancellationRequested) return;
                        _logService.Trace("Mapping to UI layer...");
                        _recentlyUsedDirectories = Mapper.Map<List<DirectoryModel>>(servicesRecentlyUsedDirectories);

                        if (_openCancellationTokenSource.IsCancellationRequested) return;
                        OnPropertyChanged(nameof(RecentlyUsedDirectories));
                    }

                    if (_openCancellationTokenSource.IsCancellationRequested) return;
                    OnPropertyChanged(nameof(Filenames));
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

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

                // save the last selected filename for this folder
                SaveLastSelectedFilename();

                // clear the current image
                _current = null;

                // redraw the image on a background thread
                LoadImage(value);

                // cache the next image on a background thread
                CacheImage(value + 1);

                OnPropertyChanged();
            }
        }

        private void SaveLastSelectedFilename()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Getting current folder...");
                var folder = _recentlyUsedDirectories[0];
                _logService.Trace($"Current folder is \"{folder.Path}\"");

                // save this file as the last used file for this folder
                _logService.Trace($"Last selected file in folder \"{folder.Path}\" is \"{_current?.Filename}\"");
                folder.Filename = _current?.Filename;

                // map to the service layer
                _logService.Trace("Saving recently used folder change...");
                var folders = Mapper.Map<List<Services.Models.FolderModel>>(_recentlyUsedDirectories);
                _recentlyUsedDirectoriesService.Save(folders);
            }
            finally
            {
                _logService.TraceExit();
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
                if (_current != null)
                {
                    // update the value
                    _current.Rotation = value;

                    // redraw the image
                    LoadImage(_position);

                    OnPropertyChanged();
                }
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
                using (var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    lock (Image)
                    {
                        _logService.Trace($"Saving \"{Filename}\" to \"{filename}\"...");
                        Image.Save(fileStream, ImageFormat.Jpeg);
                    }
                }

                // save the metadata
                var metadata = new Services.Models.Metadata
                {
                    Caption = Caption,
                    CaptionAlignment = CaptionAlignment,
                    Colour = Colour.ToArgb(),
                    FontBold = FontBold,
                    FontFamily = FontName,
                    FontSize = FontSize,
                    FontType = FontType,
                    Latitude = Latitude,
                    Longitude = Longitude,
                    Rotation = Rotation
                };
                _imageMetadataService.Save(metadata, Filename);

                // do we need to flag it as saved?
                if (!_current.Saved)
                {
                    // flag that the current image has metadata
                    _current.MetadataExists = true;

                    // flag that the current image has been saved
                    _current.Saved = true;

                    // reload the preview
                    Task.Factory.StartNew(() => PreviewThread(_current, _openCancellationTokenSource.Token), _openCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current)
                        .ContinueWith(ExceptionHandler, _openCancellationTokenSource.Token, TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Color? SecondColour => _configurationService.SecondColour;

        public Image SecondColourImage => _secondColourImage;

        public FormWindowState WindowState
        {
            get => _configurationService.WindowState;
            set
            {
                // ignore when the form is minimized
                if (value == FormWindowState.Minimized) return;

                // only process changes
                if (_configurationService.WindowState == value) return;

                // save the new value
                _configurationService.WindowState = value;

                OnPropertyChanged();
            }
        }
    }
}