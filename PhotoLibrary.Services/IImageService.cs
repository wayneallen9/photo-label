using System.Drawing;

namespace PhotoLibrary.Services
{
    public interface IImageService
    {
        Image Caption(Image original, string caption, Font font, Brush brush, Point location);
        string GetDateTaken(string filename);
        Image Get(string filename);
    }
}