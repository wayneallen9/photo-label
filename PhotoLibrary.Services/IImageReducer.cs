using System.Drawing;
using System.IO;

namespace PhotoLabel.Services
{
    public interface IImageReducer
    {
        Stream Reduce(Bitmap image);
    }
}