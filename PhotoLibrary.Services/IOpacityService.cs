using System.Windows.Media;

namespace PhotoLabel.Services
{
    public interface IOpacityService
    {
        string GetOpacity(Color color);
        Color SetOpacity(Color color, string percentage);
    }
}