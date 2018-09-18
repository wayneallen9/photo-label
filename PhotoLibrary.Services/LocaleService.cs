using System.Globalization;
using System.Text.RegularExpressions;
namespace PhotoLabel.Services
{
    public class LocaleService : ILocaleService
    {
        #region variables
        private readonly ILogService _logService;
        #endregion

        public LocaleService(
            ILogService logService)
        {
            // save dependency injections
            _logService = logService;
        }

        public bool PercentageTryParse(string text, out decimal percentage)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Extracting percentage from \"{text}\"...");

                _logService.Trace("Getting current culture...");
                var currentCulture = CultureInfo.CurrentCulture;
                _logService.Trace($"Current culture is \"{currentCulture.Name}\"");

                // get the current number formant
                _logService.Trace("Getting number format for current culture...");
                var numberFormatInfo = currentCulture.NumberFormat;

                // get the percentage sign for that culture
                var percentageSign = numberFormatInfo.PercentSymbol;
                _logService.Trace($"Current percentage sign is {numberFormatInfo.PercentSymbol}");

                // strip all of the expected non-numeric values
                text = Regex.Replace(text, $"^(.*?){percentageSign}", "$1");
                text = text.Replace(numberFormatInfo.PercentDecimalSeparator, numberFormatInfo.NumberDecimalSeparator);
                text = text.Replace(numberFormatInfo.PercentGroupSeparator, string.Empty);

                return decimal.TryParse(text, out percentage);
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}