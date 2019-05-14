using Shared;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace PhotoLabel.Wpf
{
    public class PercentageValidationRule : ValidationRule
    {
        public PercentageValidationRule()
        {
            // initialise dependencies
            _logger = Injector.Get<ILogger>();

            // initialise variables
            Maximum = 100;
        }

        public int Maximum { get; set; }

        public int Minimum { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            using (_logger.Block())
            {
                try
                {
                    // the value cannot be null
                    if (value == null) return new ValidationResult(true, null);

                    // the word "Off" is valid
                    if (Regex.IsMatch(value.ToString(), "^off$", RegexOptions.IgnoreCase)) return new ValidationResult(true, null);

                    // create the Regex to validate
                    var regex = new Regex($@"^\s*(\d{{1,3}})\s*{cultureInfo.NumberFormat.PercentSymbol}?\s*$");

                    // validate the format
                    var match = regex.Match(value.ToString());
                    if (!match.Success) return new ValidationResult(false, "Invalid percentage");

                    // validate that it is between min and max
                    if (!int.TryParse(match.Groups[1].Value, out var result))
                        return new ValidationResult(false, "Invalid percentage");
                    if (result < Minimum || result > Maximum)
                        return new ValidationResult(false, "Percentage is out of range");

                    return new ValidationResult(true, null);
                }
                catch (Exception)
                {
                    return new ValidationResult(false, "Invalid percentage");
                }
            }
        }

        #region variables
        private readonly ILogger _logger;


        #endregion
    }
}