using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PhotoLabel.Services
{
    public class LogService : IDisposable, ILogService
    {
        #region variables
        private bool _disposedValue; // To detect redundant calls
        private readonly IDictionary<int, int> _indent;
        private readonly BufferBlock<string> _logEntriesBufferBlock;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        #endregion

        public LogService()
        {
            // initialise variables
            _indent = new Dictionary<int, int>();
            _logEntriesBufferBlock = new BufferBlock<string>();

            // start the writer
            WriteLogAsync(_logEntriesBufferBlock);
        }

        public void TraceEnter([CallerMemberName] string callerMemberName = "")
        {
            // get the id of the current thread
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;

            // get the indent for this thread
            var indent = _indent.ContainsKey(currentThreadId) ? _indent[currentThreadId] : 0;

            // log the entry
            _logEntriesBufferBlock.Post($"{callerMemberName} {new string('\t', indent)}Enter");

            // now increase the indentation
            _indent[currentThreadId] = ++indent;
        }

        public void TraceExit([CallerMemberName] string callerMemberName = "")
        {
            // get the id of the current thread
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;

            // get the indent for this thread
            var indent = _indent[currentThreadId] - 1;

            // log the exit
            _logEntriesBufferBlock.Post($"{ callerMemberName} {new string('\t', indent)}Exit");

            // save the new value
            _indent[currentThreadId] = indent;
        }

        public void TraceExit(Stopwatch stopWatch, [CallerMemberName] string callerMemberName = "")
        {
            // stop the stop watch
            stopWatch.Stop();

            // get the id of the current thread
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;

            // get the indent for this thread
            var indent = _indent[currentThreadId] - 1;

            // log the exit
            _logEntriesBufferBlock.Post($"{callerMemberName} {new string('\t', indent)}Exit - {stopWatch.ElapsedMilliseconds}ms");

            // save the new value
            _indent[currentThreadId] = indent;
        }

        public void Error(Exception ex)
        {
            try
            {
                Logger.Error(ex);
            }
            catch (Exception)
            {
                // ignore any exceptions when logging
            }
        }

        public void Trace(string message, [CallerMemberName] string callerMemberName="")
        {
            // get the id of the current thread
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;

            // get the indent for this thread
            var indent = _indent.ContainsKey(currentThreadId) ? _indent[currentThreadId] : 0;

            _logEntriesBufferBlock.Post($"{callerMemberName} {new string('\t', indent)}{message}");
        }

        private async Task WriteLogAsync(ISourceBlock<string> sourceBlock)
        {
            // read from the source buffer until there are no more entries
            while (await sourceBlock.OutputAvailableAsync())
            {
                // get the next entry
                var logEntry = sourceBlock.Receive();

                // output this entry
                Logger.Trace(logEntry);
            }
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // flag the logging as complete
                    _logEntriesBufferBlock.Complete();
                }

                _disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}