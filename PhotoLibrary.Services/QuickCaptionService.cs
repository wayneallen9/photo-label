using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PhotoLabel.Services
{
    public class QuickCaptionService : IQuickCaptionService
    {
        #region variables

        private string _cachedFilename;
        private Models.Metadata _cachedImage;
        private readonly IDictionary<string, Models.Metadata> _images;
        private readonly ILogService _logService;
        private readonly IList<IQuickCaptionObserver> _observers;

        #endregion

        public QuickCaptionService(
            ILogService logService)
        {
            // save dependency injections
            _logService = logService;

            // initialise variables
            _images = new Dictionary<string, Models.Metadata>();
            _observers = new List<IQuickCaptionObserver>();
        }

        public void Add(string filename, Models.Metadata image)
        {
            if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentNullException(nameof(filename));

            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if a date and caption have been specified...");
                if (string.IsNullOrWhiteSpace(image.DateTaken) || string.IsNullOrWhiteSpace(image.Caption))
                {
                    _logService.Trace(@"Either the date or caption has not been specified.  Exiting...");

                    return;
                }

                _logService.Trace($@"Adding ""{filename}"" to dictionary of images with caches...");
                _images[filename] = image;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private IList<string> GetQuickCaptions()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Creating list to return...");
                var list = new List<string>();

                _logService.Trace("Checking if there is a cached image...");
                if (_cachedImage == null)
                {
                    _logService.Trace("There is no cached image.  Returning...");
                    return list;
                }

                _logService.Trace($@"Adding ""{_cachedFilename}"" as first caption...");
                list.Add(Path.GetFileNameWithoutExtension(_cachedFilename));

                _logService.Trace($@"Checking if ""{_cachedFilename}"" has a date taken...");
                if (string.IsNullOrWhiteSpace(_cachedImage.DateTaken))
                {
                    _logService.Trace($@"""{_cachedFilename}"" does not have a date taken.  Exiting...");
                    return list;
                }

                _logService.Trace($@"Retrieving all captions for ""{_cachedImage.DateTaken}""...");
                list.AddRange(_images.Values.Where(i => i.DateTaken == _cachedImage.DateTaken).Select(i => i.Caption)
                    .Distinct().OrderBy(c => c));

                return list;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void Clear()
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Clearing list...");
                _cachedImage = null;
                _images.Clear();

                _logService.Trace($@"Notifying {_observers.Count} observers that list has been cleared...");
                foreach (var observer in _observers) observer.OnClear();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void Remove(string filename)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Checking if there is quick caption information for ""{filename}""...");
                if (!_images.ContainsKey(filename))
                {
                    _logService.Trace($@"There is no quick caption information for ""{filename}"".  Exiting...");
                    return;
                }

                _logService.Trace($@"Removing quick caption information for ""{filename}""...");
                _images.Remove(filename);

                _logService.Trace($@"Checking if ""{filename}"" is the cached file...");
                if (_cachedFilename != filename) return;

                _logService.Trace($@"""{filename}"" is the cached file.  Removing...");
                _cachedFilename = null;
                _cachedImage = null;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public void Switch(string filename, Models.Metadata image)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if filename has changed...");
                if (_cachedFilename == filename)
                {
                    _logService.Trace("Filename has not changed.  Exiting...");
                    return;
                }

                _logService.Trace("Caching filename...");
                _cachedFilename = filename;
                _cachedImage = image;

                _logService.Trace($@"Building caption list for ""{filename}""...");
                var captions = GetQuickCaptions();

                _logService.Trace($"Notifying {_observers.Count} observers of {captions.Count} captions...");
                foreach (var observer in _observers) Notify(observer, captions);
            }
            catch (Exception ex)
            {
                _logService.Trace($@"Notifying {_observers.Count} observers of error...");
                foreach (var observer in _observers) observer.OnError(ex);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        private void Notify(IQuickCaptionObserver observer, IList<string> captions)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Clearing current list...");
                observer.OnClear();

                _logService.Trace($"Notifying of {captions.Count} captions...");
                foreach (var caption in captions)
                {
                    _logService.Trace($@"Notifying of ""{caption}""...");
                    observer.OnNext(caption);
                }

                _logService.Trace("All captions have been returned...");
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public IDisposable Subscribe(IQuickCaptionObserver observer)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if observer is already subscribed...");
                if (_observers.Contains(observer)) return new Unsubscriber<IQuickCaptionObserver>(_observers, observer);

                _logService.Trace("Observer is not subscribed.  Subscribing...");
                _observers.Add(observer);

                _logService.Trace("Notifying observer of current state...");
                var captions = GetQuickCaptions();
                Notify(observer, captions);

                return new Unsubscriber<IQuickCaptionObserver>(_observers, observer);
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}