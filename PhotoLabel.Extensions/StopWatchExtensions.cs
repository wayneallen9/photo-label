using System.Diagnostics;

namespace PhotoLabel.Extensions
{
    public static class StopWatchExtensions
    {
        public static Stopwatch StartStopwatch(this Stopwatch stopWatch)
        {
            // start the stop watch
            stopWatch.Start();

            return stopWatch;
        }
    }
}