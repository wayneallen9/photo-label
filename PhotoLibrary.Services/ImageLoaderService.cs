using System.Collections.Concurrent;
using System.Drawing;
namespace PhotoLabel.Services
{
    public class ImageLoaderService : IImageLoaderService
    {
        #region variables
        private readonly ConcurrentDictionary<string, ImageLoader> _loaders;
        private readonly ILogService _logService;
        private CachedImage _primary;
        private readonly object _primaryLock = new object();
        private CachedImage _secondary;
        private readonly object _secondaryLock = new object();
        private CachedImage _tertiary;
        private readonly object _tertiaryLock = new object();
        #endregion

        public ImageLoaderService(
            ILogService logService)
        {
            // save the dependency injections
            _logService = logService;

            // initialise the variables
            _loaders = new ConcurrentDictionary<string, ImageLoader>();
        }

        public Image Load(string filename)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Checking if \"{filename}\" is the primary cached file...");
                lock (_primaryLock)
                {
                    if (_primary?.Filename == filename)
                    {
                        _logService.Trace($"\"{filename}\" is the primary cached file.  Returning...");
                        return _primary.Image;
                    }
                }

                _logService.Trace($"Checking if \"{filename}\" is the secondary cached file...");
                lock (_secondaryLock)
                {
                    if (_secondary?.Filename == filename)
                    {
                        _logService.Trace($"\"{filename}\" is the secondary cached file.  Returning...");
                        return _secondary.Image;
                    }
                }

                _logService.Trace($"Checking if \"{filename}\" is the tertiary cached file...");
                lock (_tertiaryLock)
                {
                    if (_tertiary?.Filename == filename)
                    {
                        _logService.Trace($"\"{filename}\" is the tertiary cached file.  Returning...");
                        return _tertiary.Image;
                    }
                }

                _logService.Trace($"Get or create the loader for \"{filename}\"...");
                var loader = _loaders.GetOrAdd(filename, new ImageLoader(filename));
                var image = loader.Image;
                lock (_primaryLock)
                lock (_secondaryLock)
                lock (_tertiaryLock)
                {
                    // check that another thread hasn't already updated the cache
                    if (_primary?.Filename != filename && 
                        _secondary?.Filename != filename &&
                        _tertiary?.Filename != filename)
                    {
                        // promote the existing caches
                        _primary = _secondary;
                        _secondary = _tertiary;

                        // create a new secondary cache
                        _tertiary = new CachedImage
                        {
                            Filename = filename,
                            Image = image
                        };
                    }
                }

                // remove the loader from the list of active loaders
                _loaders.TryRemove(filename, out ImageLoader value);

                return image;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private class CachedImage
        {
            public string Filename { get; set; }
            public Image Image { get; set; }
        }

        private class ImageLoader
        {

            #region variables
            private Image _image;
            private readonly object _imageLock = new object();
            #endregion

            public ImageLoader(string filename)
            {
                // save the filename
                Filename = filename;
            }

            public string Filename { get; }

            public Image Image
            {
                get
                {
                    lock (_imageLock)
                    {
                        // do we need to load the image?
                        if (_image == null)
                        {
                            _image = Image.FromFile(Filename);
                        }
                    }

                    return _image;
                }
            }
        }
    }
}