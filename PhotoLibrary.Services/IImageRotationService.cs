using System.Drawing;

namespace PhotoLabel.Services
{
    public interface IImageRotationService
    {
        Bitmap Rotate(Bitmap bitmap, Rotations rotation);
    }
}