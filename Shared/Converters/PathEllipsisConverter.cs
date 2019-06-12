using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using FontFamily = System.Windows.Media.FontFamily;
using SystemFonts = System.Windows.SystemFonts;

namespace Shared.Converters
{
    public class PathEllipsisConverter : IValueConverter
    {
        #region variables
        #endregion

        public PathEllipsisConverter()
        {
            // set the default values
            FontFamily = SystemFonts.MenuFontFamily;
            FontSize = SystemFonts.MenuFontSize;
        }

        public FontFamily FontFamily { get; set; }

        public double FontSize { get; set; }

        public int Width { get; set; } = 100;

        #region IValueConverter
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // handle nulls
            if (value == null) return null;

            // make a copy of the original string
            var copy = string.Copy(value.ToString());

            // create the font from the Windows Media font
            using (var font = new Font(FontFamily.Source, (float) FontSize))
            {

                // create the maximum size for the new value
                var size = new Size(Width, 1024);

                // now add the ellipsis
                TextRenderer.MeasureText(copy, font, size,
                    TextFormatFlags.ModifyString | TextFormatFlags.PathEllipsis);
            }

            return copy.Substring(0, copy.IndexOf('\0'));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
        #endregion
    }
}