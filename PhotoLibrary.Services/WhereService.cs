using System.Diagnostics;
using Shared;
using Shared.Attributes;

namespace PhotoLabel.Services
{
    [Singleton]
    public class WhereService : IWhereService
    {
        #region variables

        private readonly IConfigurationService _configurationService;
        private readonly ILogger _logger;
        #endregion

        public WhereService(
            IConfigurationService configurationService,
            ILogger logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }

        public void Open(float latitude, float longitude)
        {
            using (var logger = _logger.Block()) {
                logger.Trace($"Opening location at {latitude}, {longitude}...");
                Process.Start(string.Format(_configurationService.WhereUrl, latitude, longitude));

            }
        }
    }
}