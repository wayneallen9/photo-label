using System.Drawing;

namespace PhotoLabel.Services
{
    public interface IImageLoaderService
    {
        Image Load(string filename);
    }
}