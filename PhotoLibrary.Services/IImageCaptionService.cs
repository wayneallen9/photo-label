using System.Drawing;
using System.Threading;

namespace PhotoLabel.Services
{
    public interface IImageCaptionService
    {
        Bitmap Caption(Bitmap original, string caption, bool? appendDateTakenToCaption, string dateTaken, Rotations rotation, CaptionAlignments? captionAlignment, string fontName, float fontSize, string fontType, bool fontBold, Brush brush, Color backgroundColour, CancellationToken cancellationToken);
    }
}