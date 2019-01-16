using System.Globalization;
using System.Text.RegularExpressions;

namespace PhotoLabel.Services
{
    public class PercentageService : IPercentageService
    {
        #region variables
        private readonly ILogService _logService;
        private readonly NumberFormatInfo _numberFormatInfo;
        #endregion

        public PercentageService(
            ILogService logService)
        {
            // save dependency injections
            _logService = logService;

            // create the format to use for percentages
            _numberFormatInfo = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
            _numberFormatInfo.PercentDecimalDigits = 0;
        }

        public float ConvertToFloat(string percentage)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Converting ""{percentage}"" to float...");
                var percentageValue = Regex.Replace(percentage, $@"[{_numberFormatInfo.PercentSymbol}\s]", string.Empty);

                return float.Parse(percentageValue) / 100;
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public string ConvertToString(float percentage)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Converting {percentage} to string...");
                return percentage.ToString("P", _numberFormatInfo);
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}