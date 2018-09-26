using System;
using System.Drawing;
namespace PhotoLabel.Services.Models
{
    [Serializable]
    public class Metadata
    {
        public Color Color { get; set; }
        public string Caption { get; set; }
        public CaptionAlignments CaptionAlignment { get; set; }
        public Font Font { get; set; }
        public float? Latitude { get; set;}
        public float? Longitude { get; set; }
        public Rotations Rotation { get; set; }
    }
}