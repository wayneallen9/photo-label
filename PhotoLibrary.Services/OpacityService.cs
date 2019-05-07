using System;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace PhotoLabel.Services
{
    public class OpacityService : IOpacityService
    {
        public OpacityService(
            ILogService logService)
        {
            // save dependencies
            _logService = logService;

            // initialise variables
            _regex = new Regex(@"^\s*(\d{1,3})\s*%?\s*$", RegexOptions.Compiled);
        }

        public string GetOpacity(Color color)
        {
            _logService.TraceEnter();
            try
            {
                _logService.Trace($"Converting {color.A} to a percentage...");
                var percentage = Math.Round((double) color.A / 255d * 100d, 0);

                return Math.Abs(percentage - 0) <= double.Epsilon ? "Off":$"{percentage}%";
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public Color SetOpacity(Color color, string percentage)
        {
            byte value = 0;

            _logService.TraceEnter();
            try
            {
                _logService.Trace($@"Checking that ""{percentage}"" is a valid percentage...");
                if (percentage.ToLower() != "off" && (!_regex.IsMatch(percentage) || !byte.TryParse(_regex.Match(percentage).Groups[1].Value, out value)))
                {
                    _logService.Trace($@"""{percentage}"" is not a valid percentage.  Throwing error...");
                    throw new InvalidOperationException();
                }

                byte a;
                if (percentage.ToLower() == "off")
                {
                    _logService.Trace("Color is transparent...");
                    a = 0;
                }
                else
                {
                    _logService.Trace("Calculating transparency...");
                    a = (byte)Math.Floor((double)value / 100d * 255d);
                }

                return new Color()
                {
                    A = a,
                    B = color.B,
                    G = color.G,
                    R = color.R
                };
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        #region variables

        private readonly ILogService _logService;
        private readonly Regex _regex;

        #endregion
    }
}