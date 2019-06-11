using System;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Shared;
using Shared.Attributes;

namespace PhotoLabel.Services
{
    [Singleton]
    public class OpacityService : IOpacityService
    {
        public OpacityService(
            ILogger logger)
        {
            // save dependencies
            _logger = logger;

            // initialise variables
            _regex = new Regex(@"^\s*(\d{1,3})\s*%?\s*$", RegexOptions.Compiled);
        }

        public string GetOpacity(Color color)
        {
            using (var logger = _logger.Block()) {
                logger.Trace($"Converting {color.A} to a percentage...");
                var percentage = Math.Round(color.A / 255d * 100d, 0);

                return Math.Abs(percentage - 0) <= double.Epsilon ? "Off":$"{percentage}%";
            
            }
        }

        public Color SetOpacity(Color color, string percentage)
        {
            byte value = 0;

            using (var logger = _logger.Block()) {
                logger.Trace($@"Checking that ""{percentage}"" is a valid percentage...");
                if (percentage.ToLower() != "off" && (!_regex.IsMatch(percentage) || !byte.TryParse(_regex.Match(percentage).Groups[1].Value, out value)))
                {
                    logger.Trace($@"""{percentage}"" is not a valid percentage.  Throwing error...");
                    throw new InvalidOperationException();
                }

                byte a;
                if (percentage.ToLower() == "off")
                {
                    logger.Trace("Color is transparent...");
                    a = 0;
                }
                else
                {
                    logger.Trace("Calculating transparency...");
                    a = (byte)Math.Floor(value / 100d * 255d);
                }

                return new Color()
                {
                    A = a,
                    B = color.B,
                    G = color.G,
                    R = color.R
                };
            
            }
        }

        #region variables

        private readonly ILogger _logger;
        private readonly Regex _regex;

        #endregion
    }
}