using System;

namespace PhotoLabel.Services.Models
{
    [Serializable]
    public class Metadata
    {
        public bool? AppendDateTakenToCaption { get; set; }
        public int? BackgroundColour { get; set; }
        public int Brightness { get; set; }
        public string Caption { get; set; }
        public CaptionAlignments? CaptionAlignment { get; set; }
        public int? Colour { get; set; }
        public string DateTaken { get; set; }
        public bool? FontBold { get; set; }
        public string FontFamily { get; set; }
        public float? FontSize { get; set; }
        public string FontType { get; set; }
        public ImageFormat? ImageFormat { get; set; }
        public float? Latitude { get; set;}
        public float? Longitude { get; set; }
        public Rotations? Rotation { get; set; }
    }
}