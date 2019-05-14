using System;
using System.Diagnostics;
using NLog;
using PhotoLabel.Services;

namespace Shared
{
    public class LoggerBlock : ILoggerBlock
    {
        #region variables

        private bool _disposedValue; // To detect redundant calls
        private readonly IIndentation _indentation;
        private readonly NLog.ILogger _logger;
        private readonly string _name;
        private readonly Stopwatch _stopwatch;
        #endregion

        public LoggerBlock(
            string name)
        {
            // save dependencies
            _indentation = Injector.Get<IIndentation>();
            _logger = Injector.Get<NLog.ILogger>();

            // get the name of the method that requested the log
            _name = name;

            // increment the indentation
            _indentation.Increment();

            // start the topwatch
            _stopwatch = Stopwatch.StartNew();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                // decrement the indentation
                _indentation.Decrement();

                // log that the block is exiting
                _logger.Trace($"{_indentation}Exiting {_name} - {_stopwatch.ElapsedMilliseconds}ms");
            }

            _disposedValue = true;
        }

        public void Error(Exception ex)
        {
            try
            {
                // log that an error has been found
                _logger.Error(ex, $"{_indentation}Exception Throw");
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void Trace(string message)
        {
            try
            {
                _logger.Trace($"{_indentation}{message}");
            }
            catch (Exception)
            {
                // ignored
            }
        }

        #region IDisposable
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}