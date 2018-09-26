using System;
using System.Drawing;
namespace PhotoLabel.Services.Models
{
    [Serializable]
    public class Metadata
    {
        public string Caption { get; set; }
        public CaptionAlignments CaptionAlignment { get; set; }
        public int Color { get; set; }
        public bool FontBold { get; set; }
        public string FontFamily { get; set; }
        public float FontSize { get; set; }
        public float? Latitude { get; set;}
        public float? Longitude { get; set; }
        public Rotations Rotation { get; set; }
    }
}