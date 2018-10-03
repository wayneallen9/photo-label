using PhotoLabel.Services;
using System.Drawing;
namespace PhotoLabel.Models
{
    public class ImageModel
    {
        public string Caption { get; set; }
        public CaptionAlignments? CaptionAlignment { get; set; }
        public Color? Colour { get; set; }
        public bool ExifLoaded { get; set; }
        public string Filename { get; set; }
        public Font Font { get; set; }
        public float? Latitude { get; set; }
        public float? Longitude { get; set; }
        public bool MetadataExists { get; set; }
        public bool MetadataLoaded { get; set; }
        public Rotations? Rotation { get; set; }
        public bool Saved { get; set; }
    }
}