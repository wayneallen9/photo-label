using System.Collections.Generic;
using System.Drawing;

namespace PhotoLabel.Services
{
    public interface ILineWrapService
    {
        List<string> WrapToFitFromBottom(Graphics graphics, Size imageSize, string source, Font font);
        List<string> WrapToFitFromTop(Graphics graphics, Size imageSize, string source, Font font);
    }
}