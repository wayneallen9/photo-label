using PhotoLabel.Services.Models;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace PhotoLabel.Services
{
    public interface IImageService
    {
        Image Caption(Image image, string caption, CaptionAlignments captionAlignment, string fontName, float fontSize, string fontType, bool fontBold, Brush brush, Color backgroundColor, Rotations rotation, int brightness, CancellationToken cancellationToken);
        Image Circle(Color color, int width, int height);
        IList<string> Find(string directory);
        ExifData GetExifData(string filename);
        Image Get(string filename);
        Image Get(string filename, int width, int height);
        Image Overlay(Image image, Image overlay, int x, int y);
        void Save(Image image, string filename, ImageFormat format);
    }
}