using System.Drawing;

namespace PhotoLabel.Wpf.Extensions
{
    public static class ColorExtensions
    {
        public static System.Windows.Media.Color ToWindowsMediaColor(this Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static Color ToDrawingColor(this System.Windows.Media.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}