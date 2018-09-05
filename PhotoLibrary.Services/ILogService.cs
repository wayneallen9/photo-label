using System;
namespace PhotoLibrary.Services
{
    public interface ILogService
    {
        void Trace(string message);
        void TraceEnter();
        void Error(Exception ex);
        void TraceExit();
    }
}