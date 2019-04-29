using System.Drawing;
using System.Windows.Forms;

namespace PhotoLabel.Services
{
    public interface IConfigurationService
    {
        bool AppendDateTakenToCaption { get; set; }
        Color BackgroundColour { get; set; }
        Color? BackgroundSecondColour { get; set; }
        CaptionAlignments CaptionAlignment { get; set; }
        Color Colour { get; set; }
        bool FontBold { get; set; }
        string FontName { get; set; }
        float FontSize { get; set; }
        string FontType { get; set; }
        ImageFormat ImageFormat { get; set; }
        string OutputPath { get; set; }
        Color? SecondColour { get; set; }
        string WhereUrl { get; }

        FormWindowState WindowState { get; set; }
    }
}