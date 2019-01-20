using NLog;
using System;
namespace PhotoLabel.Services
{
    public class LogService : ILogService
    {
        #region variables
        private int _indent;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        #endregion

        public void TraceEnter()
        {
            // log the entry
            Logger.Trace($"{new string('\t', _indent)}Enter");

            // now increase the indentation
            _indent++;
        }

        public void TraceExit()
        {
            // decrease the indentation
            if (_indent > 0) _indent--;

            // log the exit
            Logger.Trace($"{new string('\t', _indent)}Exit");
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
            Logger.Trace($"{new string('\t', _indent)}{message}");
        }
    }
}