using System.Drawing;
using PhotoLabel.Services;

namespace PhotoLabel.Models
{
    public class ImageModel
    {
        public bool? AppendDateTakenToCaption { get; set; }
        public Color? BackgroundColour { get; set; }
        public string Caption { get; set; }
        public CaptionAlignments? CaptionAlignment { get; set; }
        public Color? Colour { get; set; }
        public string DateTaken { get; set; }
        public string Filename { get; set; }
        public bool? FontBold { get; set; }
        public string FontName { get; set; }
        public float? FontSize { get; set; }
        public string FontType { get; set; }
        public ImageFormat? ImageFormat { get; set; }
        public bool IsExifLoaded { get; set; }
        public bool IsMetadataLoaded { get; set; }
        public bool IsPreviewLoaded { get; set; }
        public float? Latitude { get; set; }
        public float? Longitude { get; set; }
        public string OutputFilename { get; set; }
        public Rotations? Rotation { get; set; }
        public bool IsSaved { get; set; }
    }
}