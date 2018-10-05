using PhotoLabel.Services.Models;
using System.Collections.Generic;
using System.Drawing;

namespace PhotoLabel.Services
{
    public interface IImageService
    {
        Image Caption(Image image, string caption, CaptionAlignments captionAlignment, Font font, Brush brush, Rotations rotation);
        Image Caption(string filename, string caption, CaptionAlignments captionAlignment, Font font, Brush brush, Rotations rotation);
        Image Circle(Color color, int width, int height);
        IList<string> Find(string directory);
        ExifData GetExifData(string filename);
        Image Get(string filename);
        Image Get(string filename, int width, int height);
        Image Overlay(Image image, Image overlay, int x, int y);
        Image Overlay(string filename, int width, int height, Image overlay, int x, int y);
    }
}