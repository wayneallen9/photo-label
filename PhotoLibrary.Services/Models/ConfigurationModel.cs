namespace PhotoLabel.Services.Models
{
    public class ConfigurationModel
    {
        public CaptionAlignments CaptionAlignment { get; set; }
        public int Colour { get; set; }
        public bool FontBold { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public string FontType { get; set; }
        public string OutputPath { get; set; }
        public int? SecondColour { get; set; }
    }
}