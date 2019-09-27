using PhotoLabel.Services.Models;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;

namespace PhotoLabel.Services
{
    public interface IImageService
    {
        Bitmap Brightness(Image image, int brightness);
        Bitmap Caption(Bitmap image, string caption, bool? appendDateTakenToCaption, string dateTaken, Rotations rotation, CaptionAlignments? captionAlignment, string fontName, float fontSize, string fontType, bool fontBold, Brush brush, Color backgroundColor, bool useCanvas, int? canvasWidth, int? canvasHeight, CancellationToken cancellationToken);
        List<string> Find(string folderPath);
        Bitmap Get(string filename, int width, int height);
        ExifData GetExifData(string filename);
        string GetFilename(string outputPath, string imagePath, ImageFormat imageFormat);
        Bitmap Overlay(Bitmap image, Image overlay, int x, int y);
        Stream ReduceQuality(Bitmap image, long quality);
        Bitmap Resize(Bitmap image, int width, int height);
        void Save(Bitmap image, string filename, ImageFormat format);
    }
}