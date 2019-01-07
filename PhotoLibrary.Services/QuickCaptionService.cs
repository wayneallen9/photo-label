using System.Collections.Generic;
using System.Linq;

namespace PhotoLabel.Services
{
    public class QuickCaptionService : IQuickCaptionService
    {
        #region variables

        private readonly ILogService _logService;
        private readonly IDictionary<string, IList<string>> _quickCaptions;
        #endregion

        public QuickCaptionService(
            ILogService logService)
        {
            // save dependency injections
            _logService = logService;

            // initialise variables
            _quickCaptions = new Dictionary<string, IList<string>>();
        }

        public void Add(string date, string caption)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if a date and caption have been specified...");
                if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(caption))
                {
                    _logService.Trace("No date has been specified.  Exiting...");
                    return;
                }

                _logService.Trace($@"Adding caption ""{caption}"" for date ""{date}""...");
                if (!_quickCaptions.ContainsKey(date)) _quickCaptions.Add(date, new List<string>());
                if (!_quickCaptions[date].Contains(caption)) _quickCaptions[date].Add(caption);
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
                _logService.Trace($"Clearing {_quickCaptions.Count} dates...");
                _quickCaptions.Clear();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public IEnumerable<string> Get(string date)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if a date has been specified...");
                if (string.IsNullOrWhiteSpace(date))
                {
                    _logService.Trace("No date has been specified.  Returning...");
                    return new List<string>();
                }

                _logService.Trace($@"Checking if there are captions for ""{date}""...");
                if (_quickCaptions.ContainsKey(date))
                {
                    _logService.Trace($@"Returning {_quickCaptions[date].Count} captions for ""{date}""...");
                    return _quickCaptions[date].OrderBy(c => c);
                }
                else
                {
                    _logService.Trace($@"No captions found for ""{date}"".  Returning...");
                    return new List<string>();
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}