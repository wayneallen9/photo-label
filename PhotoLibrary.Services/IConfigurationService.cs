using System.Drawing;
using System.Windows.Forms;

namespace PhotoLabel.Services
{
    public interface IConfigurationService
    {
        CaptionAlignments CaptionAlignment { get; set; }
        Color Colour { get; set; }
        bool FontBold { get; set; }
        string FontName { get; set; }
        float FontSize { get; set; }
        string FontType { get; set; }
        bool LoadLastFolder { get; set; }
        string OutputPath { get; set; }
        Color? SecondColour { get; set; }
        FormWindowState WindowState { get; set; }
    }
}