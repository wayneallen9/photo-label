using System;
using System.Diagnostics;

namespace PhotoLabel.Services
{
    public interface ILogService
    {
        void Trace(string message);
        void TraceEnter();
        void Error(Exception ex);
        void TraceExit();
        void TraceExit(Stopwatch stopWatch);
    }
}