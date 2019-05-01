using NLog;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PhotoLabel.Services
{
    public class LogService : ILogService
    {
        #region variables
        private readonly IIndentationService _indentationService;
        private readonly ILogger _logger;
        #endregion

        public LogService(
            IIndentationService indentationService,
            ILogger logger)
        {
            // save dependencies
            _indentationService = indentationService;
            _logger = logger;
        }

        public void TraceEnter([CallerMemberName] string callerMemberName = "")
        {
            try
            {
                // write the message
                _logger.Trace($"{callerMemberName} {_indentationService}Enter");

                // increase the indentation
                _indentationService.Increment();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void TraceExit([CallerMemberName] string callerMemberName = "")
        {
            try
            {
                // decrease the indentation
                _indentationService.Decrement();

                // write the message
                _logger.Trace($"{callerMemberName} {_indentationService}Exit");
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void TraceExit(Stopwatch stopWatch, [CallerMemberName] string callerMemberName = "")
        {
            try
            {
                // stop the stop watch
                stopWatch.Stop();

                // decrement the indentation
                _indentationService.Decrement();

                // write the message
                _logger.Trace($"{callerMemberName} {_indentationService}Exit - {stopWatch.ElapsedMilliseconds}ms");
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void Error(Exception ex)
        {
            try
            {
                _logger.Error(ex);
            }
            catch (Exception)
            {
                // ignore any exceptions when logging
            }
        }

        public void Trace(string message, [CallerMemberName] string callerMemberName="")
        {
            // write the message
            _logger.Trace($"{callerMemberName} {_indentationService}{message}");
        }
    }
}