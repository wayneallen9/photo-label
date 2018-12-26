using System.Windows.Forms;

namespace PhotoLabel.Services.Models
{
    public class ConfigurationModel
    {
        public bool AppendDateTakenToCaption { get; set; }
        public CaptionAlignments CaptionAlignment { get; set; }
        public int Colour { get; set; }
        public bool FontBold { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public string FontType { get; set; }
        public ImageFormat ImageFormat { get; set; }
        public string OutputPath { get; set; }
        public int? SecondColour { get; set; }
        public FormWindowState WindowState {get;set;}
    }
}