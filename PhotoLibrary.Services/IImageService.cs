using PhotoLabel.Services.Models;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace PhotoLabel.Services
{
    public interface IImageService
    {
        Image Brightness(Image image, int brightness);
        Bitmap Caption(Bitmap image, string caption, CaptionAlignments captionAlignment, string fontName, float fontSize, string fontType, bool fontBold, Brush brush, Color backgroundColor, CancellationToken cancellationToken);
        IList<string> Find(string directory);
        ExifData GetExifData(string filename);
        Bitmap Get(string filename, int width, int height);
        Bitmap Overlay(Bitmap image, Image overlay, int x, int y);
        void Save(Image image, string filename, ImageFormat format);
    }
}