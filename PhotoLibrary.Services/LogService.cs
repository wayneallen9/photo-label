using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace PhotoLabel.Services
{
    public class LogService : ILogService
    {
        #region variables

        private readonly IDictionary<int, int> _indent;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        #endregion

        public LogService()
        {
            // initialise variables
            _indent = new Dictionary<int, int>();
        }

        public void TraceEnter()
        {
            // get the id of the current thread
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;

            // get the indent for this thread
            var indent = _indent.ContainsKey(currentThreadId) ? _indent[currentThreadId] : 0;

            // log the entry
            Logger.Trace($"{new string('\t', indent)}Enter");

            // now increase the indentation
            _indent[currentThreadId] = ++indent;
        }

        public void TraceExit()
        {
            // get the id of the current thread
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;

            // get the indent for this thread
            var indent = _indent[currentThreadId] - 1;

            // log the exit
            Logger.Trace($"{new string('\t', indent)}Exit");

            // save the new value
            _indent[currentThreadId] = indent;
        }

        public void TraceExit(Stopwatch stopWatch)
        {
            // stop the stop watch
            stopWatch.Stop();

            // get the id of the current thread
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;

            // get the indent for this thread
            var indent = _indent[currentThreadId] - 1;

            // log the exit
            Logger.Trace($"{new string('\t', indent)}Exit - {stopWatch.ElapsedMilliseconds}ms");

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

        public void Trace(string message)
        {
            // get the id of the current thread
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;

            // get the indent for this thread
            var indent = _indent.ContainsKey(currentThreadId) ? _indent[currentThreadId] : 0;

            Logger.Trace($"{new string('\t', indent)}{message}");
        }
    }
}