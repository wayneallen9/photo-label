using System.Drawing;

namespace PhotoLibrary.Services
{
    public interface IImageService
    {
        Image Caption(Image original, string caption, CaptionAlignments captionAlignment, Font font, Brush brush, Rotations rotation);
        string GetDateTaken(string filename);
        Image Get(string filename);
        Image Get(string filename, int width, int height);
        Image Overlay(string filename, int width, int height, Image overlay, int x, int y);
    }
}