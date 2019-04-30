using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Color = System.Windows.Media.Color;

namespace PhotoLabel.Services
{
    public interface IConfigurationService
    {
        bool AppendDateTakenToCaption { get; set; }
        Color BackgroundColour { get; set; }
        CaptionAlignments CaptionAlignment { get; set; }
        double CaptionSize { get; set; }
        Color Colour { get; set; }
        bool FontBold { get; set; }
        string FontName { get; set; }
        float FontSize { get; set; }
        string FontType { get; set; }
        ImageFormat ImageFormat { get; set; }
        string OutputPath { get; set; }
        IList<Color> RecentlyUsedBackColors { get; set; }
        string WhereUrl { get; }

        FormWindowState WindowState { get; set; }
    }
}