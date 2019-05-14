using System.Diagnostics;
using System.Runtime.CompilerServices;
using Ninject.Parameters;
using PhotoLabel.Services;
using Shared.Attributes;

namespace Shared
{
    [Singleton]
    public class Logger : ILogger
    {
        #region variables

        private readonly NLog.ILogger _logger;
        #endregion

        public Logger(
            NLog.ILogger logger)
        {
            // save dependencies
            _logger = logger;
        }

        public ILoggerBlock Block()
        {
            // create dependencies
            var indentation = Injector.Get<IIndentation>();

            // get the calling method name
            var frame = new StackTrace().GetFrame(1);
            var methodName = $"{frame.GetMethod().DeclaringType}.{frame.GetMethod().Name}";

            // now log the entry
            _logger.Trace($"{indentation}Entering {methodName}");

            // create a new block
            var nameParameter = new ConstructorArgument("name", methodName);
            return Injector.Get<ILoggerBlock>(nameParameter);
        }
    }
}