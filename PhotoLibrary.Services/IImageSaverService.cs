using System.Drawing;

namespace PhotoLabel.Services
{
    public interface IImageSaverService
    {
        void Save(Bitmap bitmap, string filename, ImageFormat imageFormat);
    }
}