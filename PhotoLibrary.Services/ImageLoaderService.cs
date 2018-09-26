using System;
using System.Drawing;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
namespace PhotoLabel.Services
{
    public class ImageLoaderService : IImageLoaderService
    {
        #region variables
        private readonly ILogService _logService;
        private readonly MemoryCache _fileMemoryCache;
        #endregion

        public ImageLoaderService(
            ILogService logService)
        {
            // save the dependency injections
            _logService = logService;

            // initialise the variables
            _fileMemoryCache = MemoryCache.Default;
        }

        public Image Load(string filename)
        {
            ImageLoader imageLoader;

            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Checking if loader for \"{filename}\" exists in cache...");
                if (_fileMemoryCache.Contains(filename))
                {
                    _logService.Trace($"Serving \"{filename}\" from cache...");
                    return ((ImageLoader)_fileMemoryCache[filename]).Image;
                }

                lock (_fileMemoryCache)
                {
                    _logService.Trace($"Checking loader for \"{filename}\" has not been created on another thread...");
                    if (_fileMemoryCache.Contains(filename)) return ((ImageLoader)_fileMemoryCache[filename]).Image;

                    _logService.Trace($"Creating loader for \"{filename}\"...");
                    imageLoader = new ImageLoader(filename);

                    _logService.Trace($"Adding loader for \"{filename}\" to the cache...");
                    _fileMemoryCache.Add(filename, imageLoader, new CacheItemPolicy
                    {
                        SlidingExpiration = TimeSpan.FromHours(1)
                    });
                }

                return imageLoader.Image;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private class ImageLoader
        {

            #region variables
            private Image _image;
            private readonly ManualResetEvent _manualResetEvent;
            #endregion

            public ImageLoader(string filename)
            {
                // save the filename
                Filename = filename;

                // create the signal
                _manualResetEvent = new ManualResetEvent(false);

                // start loading the file on another thread
                var task = new Task(ImageThread, TaskCreationOptions.LongRunning);
                task.Start();
            }

            public string Filename { get; }

            public Image Image
            {
                get
                {
                    // wait for the image to load
                    _manualResetEvent.WaitOne();

                    // return the loaded image
                    return _image;
                }
            }

            public void ImageThread()
            {
                // load from the file
                _image = Image.FromFile(Filename);

                // set the signal
                _manualResetEvent.Set();
            }
        }
    }
}
