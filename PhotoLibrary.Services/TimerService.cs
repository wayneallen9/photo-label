using System;
using System.Threading;
namespace PhotoLabel.Services
{
    public class TimerService : ITimerService
    {
        #region variables
        private DateTime _lastExecutedTime = DateTime.Now;
        private readonly ILogService _logService;
        private readonly object _pauseLock = new object();
        #endregion

        public TimerService(
            ILogService logService)
        {
            // save the dependency injections
            _logService = logService;
        }

        public void Pause(TimeSpan value)
        {
            _logService.TraceEnter();
            try
            {
                lock (_pauseLock)
                {
                    // what time does the pause end?
                    var pauseEnds = _lastExecutedTime.Add(value);

                    // how many milliseconds until the pause ends?
                    var delay = pauseEnds - DateTime.Now;

                    // wait for the pause to end
                    if (delay.TotalMilliseconds > 0) Thread.Sleep((int)delay.TotalMilliseconds);

                    // update the time of last execution
                    _lastExecutedTime = DateTime.Now;
                }
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}
