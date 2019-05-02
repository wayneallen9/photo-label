using System.Collections.Generic;
using System.Windows.Forms;

using Color = System.Windows.Media.Color;

namespace PhotoLabel.Services.Models
{
    public class Configuration
    {
        public bool AppendDateTakenToCaption { get; set; }
        public Color? BackgroundColour { get; set; }
        public CaptionAlignments CaptionAlignment { get; set; }
        public double? CaptionSize { get; set; }
        public Color? Colour { get; set; }
        public bool FontBold { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public string FontType { get; set; }
        public ImageFormat ImageFormat { get; set; }
        public ulong? MaxImageSize { get; set; }
        public string OutputPath { get; set; }
        public List<Color> RecentlyUsedBackColors { get; set; }
        public FormWindowState WindowState {get;set;}
    }
}