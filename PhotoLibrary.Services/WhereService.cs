using System.Diagnostics;

namespace PhotoLabel.Services
{
    public class WhereService : IWhereService
    {
        #region variables

        private readonly IConfigurationService _configurationService;
        private readonly ILogService _logService;
        #endregion

        public WhereService(
            IConfigurationService configurationService,
            ILogService logService)
        {
            _configurationService = configurationService;
            _logService = logService;
        }

        public void Open(float latitude, float longitude)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Opening location at {latitude}, {longitude}...");
                Process.Start(string.Format(_configurationService.WhereUrl, latitude, longitude));

            }
            finally
            {
                _logService.TraceEnter();
            }
        }
    }
}