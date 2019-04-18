using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PhotoLabel.Wpf.Extensions
{
    public static class StringExtensions
    {
        public static double ToPercentage(this string value)
        {
            // get the percentage sign
            var percentageSign = CultureInfo.CurrentCulture.NumberFormat.PercentSymbol;

            // extract everything up to the percent symbol
            var numbersMatch = Regex.Match(value, $@"^(.+?)\s*{percentageSign}\s*$");
            if (!numbersMatch.Success) throw new ArgumentException($@"""{value}"" is not a valid percentage");

            return double.Parse(numbersMatch.Groups[1].Value);
        }
    }
}