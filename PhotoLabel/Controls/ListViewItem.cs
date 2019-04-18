using PhotoLabel.Services;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using PhotoLabel.Properties;

namespace PhotoLabel.Controls
{
    public class ListViewItem : System.Windows.Forms.ListViewItem, INotifyPropertyChanged
    {
        #region constants

        private const int PreviewHeight = 128;
        private const int PreviewWidth = 128;
        #endregion

        #region delegates

        private delegate void PropertyChangedDelegate(string propertyName);
        #endregion

        #region events

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region variables

        private bool _isMetadataLoaded;
        private bool _isSaved;
        private readonly ILogService _logService;
        private readonly TaskScheduler _taskScheduler;

        #endregion

        public ListViewItem()
        {
            // get dependencies
            _logService = NinjectKernel.Get<ILogService>();
            _taskScheduler = NinjectKernel.Get<TaskScheduler>();

            // initialise variables
            _isMetadataLoaded = false;
            _isSaved = false;
        }

        public string Filename
        {
            get => Name;
            set
            {
                _logService.TraceEnter();
                try
                {
                    _logService.Trace($"Checking if value of {nameof(Filename)} has changed...");
                    if (Name == value)
                    {
                        _logService.Trace($"Value of {nameof(Filename)} has not changed.  Exiting...");
                        return;
                    }

                    _logService.Trace($"Setting new value of {nameof(Filename)}...");
                    Name = value;

                    OnPropertyChanged();
                }
                finally
                {
                    _logService.TraceExit();
                }
            }
        }

        public void LoadPreview(CancellationToken cancellationToken)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Checking if preview of ""{Filename}"" is already loaded...");
                if (Preview != null)
                {
                    _logService.Trace($@"Preview of ""{Filename}"" is already loaded.  Exiting...");

                    return;
                }

                _logService.Trace($@"Loading preview of ""{Filename}"" on high priority background thread...");
                Task<Image>.Factory.StartNew(() => LoadPreviewThread(Filename, _isSaved, _isMetadataLoaded),
                        cancellationToken, TaskCreationOptions.None, _taskScheduler)
                    .ContinueWith(PreviewLoadedSuccess, Filename, TaskContinuationOptions.OnlyOnRanToCompletion);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        protected static Image LoadPreviewThread(string filename, bool isSaved, bool isMetadataLoaded)
        {
            ILogService logService = null;

            try
            {
                // create dependencies
                var imageService = NinjectKernel.Get<IImageService>();
                logService = NinjectKernel.Get<ILogService>();

                logService.TraceEnter();

                logService.Trace($@"Loading ""{filename}""...");
                using (var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                using (var originalImage = Image.FromStream(fileStream))
                {
                    logService.Trace($@"Resizing ""{filename}"" to {PreviewWidth}x{PreviewHeight}...");
                    var preview = imageService.Resize(originalImage, PreviewWidth, PreviewHeight);

                    logService.Trace($@"Checking if ""{filename}"" has been saved...");
                    Image overlayImage;
                    if (isSaved)
                    {
                        logService.Trace($@"""{filename}"" is saved.  Adding icon...");
                        overlayImage = imageService.Overlay(preview, Resources.saved,
                            PreviewWidth - Resources.saved.Width - 4,
                            PreviewHeight - Resources.saved.Height - 4);

                        // release the original image
                        preview.Dispose();

                        return overlayImage;
                    }

                    // do we need to add the metadata icon?
                    if (!isMetadataLoaded) return preview;

                    logService.Trace($@"Metadata for ""{filename}"" has been loaded.  Adding icon...");
                    overlayImage = imageService.Overlay(preview, Resources.metadata,
                        PreviewWidth - Resources.metadata.Width - 4, PreviewHeight - Resources.metadata.Height - 4);

                    // release the original image
                    preview.Dispose();

                    return overlayImage;
                }
            }
            finally
            {
                logService?.TraceExit();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            _logService.TraceEnter();
            try
            {
                if (ListView?.InvokeRequired == true)
                {
                    ListView?.Invoke(new PropertyChangedDelegate(OnPropertyChanged), propertyName);

                    return;
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Image Preview { get; protected set; }

        private void PreviewLoadedSuccess(Task<Image> task, object state)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Updating preview image...");
                Preview = task.Result;

                OnPropertyChanged(nameof(Preview));
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}