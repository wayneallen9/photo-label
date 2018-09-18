using NLog;
using System;
namespace PhotoLabel.Services
{
    public class LogService : ILogService
    {
        #region variables
        private int indent = 0;
        private readonly static Logger logger = LogManager.GetCurrentClassLogger();
        #endregion

        public void TraceEnter()
        {
            // log the entry
            logger.Trace($"{new String('\t', indent)}Enter");

            // now increase the indentation
            indent++;
        }

        public void TraceExit()
        {
            // decrease the indentation
            indent--;

            // log the exit
            logger.Trace($"{new String('\t', indent)}Exit");
        }

        public void Error(Exception ex)
        {
            try
            {
                logger.Error(ex);
            }
            catch (Exception)
            {
                // ignore any exceptions when logging
            }
        }

        public void Trace(string message)
        {
            logger.Trace($"{new String('\t', indent)}{message}");
        }
    }
}