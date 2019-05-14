using System;

namespace Shared
{
    public interface ILoggerBlock : IDisposable
    {
        void Error(Exception ex);
        void Trace(string message);
    }
}