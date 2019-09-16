using System.Drawing;

namespace PhotoLabel.Services
{
    public interface IBrightnessService
    {
        Bitmap Adjust(Bitmap source, int brightness);
    }
}