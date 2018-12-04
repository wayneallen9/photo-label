using System;

namespace PhotoLabel
{
    public interface IInvoker
    {
        object Invoke(Delegate method, params object[] args);
        bool InvokeRequired { get; }
    }
}