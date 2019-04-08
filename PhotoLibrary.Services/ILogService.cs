using System;
using System.Diagnostics;

namespace PhotoLabel.Services
{
    public interface ILogService
    {
        void Trace(string message, string callerMemberName = "");
        void TraceEnter(string callerMemberName = "");
        void Error(Exception ex);
        void TraceExit(string callerMemberName = "");
        void TraceExit(Stopwatch stopWatch, string callerMemberName = "");
    }
}