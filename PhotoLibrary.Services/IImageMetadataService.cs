using System.Drawing;
namespace PhotoLibrary.Services
{
    public interface IImageMetadataService
    {
        bool HasMetadata(string filename);
        string LoadCaption(string filename);
        CaptionAlignments? LoadCaptionAlignment(string filename);
        Color? LoadColor(string filename);
        Font LoadFont(string filename);
        Rotations? LoadRotation(string filename);
        void Save(string caption, CaptionAlignments captionAlignment, Font font, Color color, Rotations rotation, string filename);
    }
}