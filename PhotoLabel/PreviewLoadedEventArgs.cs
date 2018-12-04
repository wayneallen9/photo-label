using System.Drawing;

namespace PhotoLabel
{
    public class PreviewLoadedEventArgs : System.EventArgs
    {
        public string Filename { get; set; }
        public Image Image { get; set; }
    }
}