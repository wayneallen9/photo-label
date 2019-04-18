using System.Drawing;

namespace PhotoLabel.CustomEventArgs
{
    public class PreviewLoadedEventArgs : System.EventArgs
    {
        public string Filename { get; set; }
        public Image Image { get; set; }
    }
}