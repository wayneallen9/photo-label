using PhotoLabel.Services.Models;
using System.Drawing;

namespace PhotoLabel.Services
{
    public interface IImageService
    {
        Image Caption(string filename, string caption, CaptionAlignments captionAlignment, Font font, Brush brush, Rotations rotation);
        ExifData GetExifData(string filename);
        Image Get(string filename);
        Image Get(string filename, int width, int height);
        Image Overlay(string filename, int width, int height, Image overlay, int x, int y);
    }
}