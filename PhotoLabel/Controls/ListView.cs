using System.Windows.Forms;
namespace PhotoLabel.Controls
{
    public class ListView : System.Windows.Forms.ListView
    {
        #region events
        public event ScrollEventHandler Scroll;
        #endregion

        public ListView()
        {
            // activate double buffering
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            // Enable the OnNotifyMessage event so we get a chance to filter out 
            // Windows messages before they get to the form's WndProc
            SetStyle(ControlStyles.EnableNotifyMessage, true);
        }

        protected override void OnNotifyMessage(Message m)
        {
            if (m.Msg != 0x14)  // WM_ERASEBKGND
            {
                base.OnNotifyMessage(m);
            }
        }

        protected virtual void OnScroll(ScrollEventArgs e)
        {
            Scroll?.Invoke(this, e);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == 0x115 || m.Msg == 0x20A) // WM_VSCROLL || WM_MOUSEWHEEL
                OnScroll(new ScrollEventArgs((ScrollEventType)(m.WParam.ToInt32() & 0xffff), 0));
        }
    }
}