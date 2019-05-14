using System.Runtime.CompilerServices;

namespace Shared
{
    public interface ILogger
    {
        ILoggerBlock Block();
    }
}