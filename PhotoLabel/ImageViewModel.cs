using PhotoLabel.Services;
using PhotoLabel.Services.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoLabel
{
    public class ImageViewModel : INotifyPropertyChanged
    {
        #region constants

        private const int PreviewHeight = 128;
        private const int PreviewWidth = 128;
        #endregion

        #region delegates

        private delegate void OnPropertyChangedDelegate(string propertyName);
        #endregion

        #region events

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region variables

        private string _caption;
        private string _dateTaken;
        private bool _hasMetadata;
        private readonly IImageMetadataService _imageMetadataService;
        private readonly ILogService _logService;
        private CancellationTokenSource _previewCancellationTokenSource;
        private readonly TaskScheduler _taskScheduler;
        private Image _preview;

        #endregion

        public ImageViewModel(string filename)
        {
            // save dependencies
            Filename = filename ?? throw new ArgumentNullException(nameof(filename));

            // get dependencies
            _imageMetadataService = NinjectKernel.Get<IImageMetadataService>();
            _logService = NinjectKernel.Get<ILogService>();
            _taskScheduler = NinjectKernel.Get<TaskScheduler>();

            // try and load the metadata
            LoadMetadata();
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

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public string DateTaken
        {
            get => _dateTaken;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(DateTaken)} has changed...");
                    if (_dateTaken == value)
                    {
                        _logService.Trace($"Value of {nameof(DateTaken)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($@"Setting value of {nameof(DateTaken)} to ""{value}""...");
                    _dateTaken = value;

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public string Filename { get; }

        public bool HasMetadata
        {
            get => _hasMetadata;
            set
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

                    _logService.Trace($@"Setting value of {nameof(HasMetadata)} to ""{value}""...");
                    _hasMetadata = value;

                    _logService.Trace($@"Reloading preview of ""{Filename}""...");
                    LoadPreview(new CancellationToken(false));

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public IInvoker Invoker { get; set; }

        public bool IsEdited { get; set; }

        public bool IsSaved { get; set; }

        private void LoadExifData()
        {
            var stopWatch = Stopwatch.StartNew();

            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Loading Exif data for ""{Filename}"" on background thread...");
                Task<ExifData>.Factory.StartNew(LoadExifDataThread, Filename, new CancellationToken(),
                        TaskCreationOptions.None, _taskScheduler)
                    .ContinueWith(LoadExifDataSuccess, null, TaskContinuationOptions.OnlyOnRanToCompletion);
            }
            finally
            {
                _logService.TraceExit(stopWatch);
            }
        }

        private void LoadExifDataSuccess(Task<ExifData> task, object state)
        {
            _logService.TraceEnter();
            try
            {
                if (task.Result == null)
                {
                    _logService.Trace($@"Unable to load Exif data for ""{Filename}"".  Exiting...");
                    return;
                }

                _logService.Trace($@"Exif data exists for ""{Filename}"".  Setting properties...");
                Caption = task.Result.Title;
                DateTaken = task.Result.DateTaken;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private static ExifData LoadExifDataThread(object state)
        {
            var filename = (string) state;

            // get dependencies
            var imageService = NinjectKernel.Get<IImageService>();
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace($@"Loading Exif data for ""{filename}""...");
                return imageService.GetExifData(filename);
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private void LoadMetadata()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Loading metadata for ""{Filename}"" on background thread...");
                Task<Metadata>.Factory.StartNew(LoadMetadataThread, Filename, new CancellationToken(false), TaskCreationOptions.None, _taskScheduler)
                    .ContinueWith(LoadMetadataSuccess, null, TaskContinuationOptions.OnlyOnRanToCompletion);

                var metadata = _imageMetadataService.Load(Filename);

                if (metadata != null)
                { 
                    _logService.Trace($@"Metadata exists for ""{Filename}"".  Setting properties...");
                    _caption = metadata.Caption;
                    _hasMetadata = true;
                }

                _logService.Trace($@"No metadata exists for ""{Filename}"".  Loading preview...");
                LoadPreview(new CancellationToken(false));
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void LoadMetadataSuccess(Task<Metadata> task, object state)
        {
            _logService.TraceEnter();
            try
            {
                if (task.Result == null)
                {
                    _logService.Trace($@"Metadata does not exist for ""{Filename}"".  Loading Exif data...");
                    LoadExifData();
                }
                else
                {
                    _logService.Trace($@"Metadata exists for ""{Filename}"".  Setting properties...");
                    _caption = task.Result.Caption;
                    _hasMetadata = true;
                }

                _logService.Trace($@"Loading preview for ""{Filename}""...");
                LoadPreview(new CancellationToken());
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private static Metadata LoadMetadataThread(object state)
        {
            var filename = (string) state;

            // get dependencies
            var imageMetadataService = NinjectKernel.Get<IImageMetadataService>();
            var logService = NinjectKernel.Get<ILogService>();

            var stopWatch = Stopwatch.StartNew();

            logService.TraceEnter();
            try
            {
                logService.Trace($@"Loading metadata for ""{filename}""...");
                return imageMetadataService.Load(filename);
            }
            finally
            {
                logService.TraceExit(stopWatch);
            }
        }

        public void LoadPreview(CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                // watch for cancellation requests
                cancellationToken.Register(LoadPreviewCancelled);

                // cancel any in progress load
                _previewCancellationTokenSource?.Cancel();

                _logService.Trace($@"Loading preview of ""{Filename}"" on background thread...");
                _previewCancellationTokenSource = new CancellationTokenSource();
                Task<Image>.Factory.StartNew(LoadPreviewThread, new object[] {Filename, IsSaved, IsEdited, HasMetadata, _previewCancellationTokenSource.Token},
                        _previewCancellationTokenSource.Token, TaskCreationOptions.None, _taskScheduler)
                    .ContinueWith(LoadPreviewSuccess, null, TaskContinuationOptions.OnlyOnRanToCompletion);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void LoadPreviewCancelled()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Cancelling loading of preview of ""{Filename}""...");
                _previewCancellationTokenSource?.Cancel();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void LoadPreviewSuccess(Task<Image> task, object state)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Setting preview of ""{Filename}""...");
                Preview = task.Result;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private static Image LoadPreviewThread(object state)
        {
            var stateArray = (object[]) state;
            var filename = (string) stateArray[0];
            var isSaved = (bool) stateArray[1];
            var isEdited = (bool) stateArray[2];
            var hasMetadata = (bool) stateArray[3];
            var cancellationToken = (CancellationToken) stateArray[4];

            ILogService logService = null;

            try
            {
                // has the request been cancelled whilst it was queued?
                if (cancellationToken.IsCancellationRequested) return null;

                // get dependencies
                var imageService = NinjectKernel.Get<IImageService>();
                logService = NinjectKernel.Get<ILogService>();

                logService.Trace($@"Loading ""{filename}"" from disk...");
                if (cancellationToken.IsCancellationRequested) return null;
                var preview = imageService.Get(filename, PreviewWidth, PreviewHeight);

                logService.Trace($@"Checking if ""{filename}"" has been saved...");
                if (cancellationToken.IsCancellationRequested) return null;
                if (isSaved)
                {
                    logService.Trace($@"Adding saved icon to ""{filename}""...");
                    return imageService.Overlay(preview, Properties.Resources.saved,
                        PreviewWidth - Properties.Resources.saved.Width - 4,
                        PreviewHeight - Properties.Resources.saved.Height - 4);
                }

                logService.Trace($@"Checking if ""{filename}"" has been edited...");
                if (cancellationToken.IsCancellationRequested) return null;
                if (isEdited)
                {
                    logService.Trace($@"Adding edited icon to ""{filename}""...");
                    return imageService.Overlay(preview, Properties.Resources.edited,
                        PreviewWidth - Properties.Resources.edited.Width - 4,
                        PreviewHeight - Properties.Resources.edited.Height - 4);
                }

                logService.Trace($@"Checking if ""{filename}"" has metadata...");
                if (cancellationToken.IsCancellationRequested) return null;
                if (hasMetadata)
                {
                    logService.Trace($@"Adding metadata icon to ""{filename}""...");
                    return imageService.Overlay(preview, Properties.Resources.metadata,
                        PreviewWidth - Properties.Resources.metadata.Width - 4,
                        PreviewHeight - Properties.Resources.metadata.Height - 4);
                }
                return preview;
            }
            finally
            {
                logService?.TraceExit();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            ILogService logService = null;

            try
            {
                // get dependencies
                logService = NinjectKernel.Get<ILogService>();

                logService.TraceEnter();

                logService.Trace("Checking if running on UI thread...");
                if (Invoker?.InvokeRequired == true)
                {
                    logService.Trace("Not running on UI thread.  Delegating to UI thread...");
                    Invoker?.Invoke(new OnPropertyChangedDelegate(OnPropertyChanged), propertyName);

                    return;
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            finally
            {
                logService?.TraceExit();
            }
        }

        public Image Preview
        {
            get => _preview;
            protected set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($@"Setting value of {nameof(Preview)}...");
                    _preview = value;

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }
    }
}