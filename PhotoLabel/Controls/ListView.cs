using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PhotoLabel.Controls
{
    public class ListView : System.Windows.Forms.ListView
    {
        #region enumerations
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private enum SBOrientation
        {
            SB_HORZ = 0x0,
            SB_VERT = 0x1,
            SB_CTL = 0x2,
            SB_BOTH = 0x3
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public enum ScrollInfoMask : uint
        {
            SIF_RANGE = 0x1,
            SIF_PAGE = 0x2,
            SIF_POS = 0x4,
            SIF_DISABLENOSCROLL = 0x8,
            SIF_TRACKPOS = 0x10,
            SIF_ALL = (SIF_RANGE | SIF_PAGE | SIF_POS | SIF_TRACKPOS),
        }
        #endregion

        #region structures
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
        private struct ScrollInfo
        {
            public uint cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }
        #endregion

        #region api
        [DllImport("user32.dll")]
        private static extern bool GetScrollInfo(IntPtr hwnd, int fnBar, ref ScrollInfo lpsi);
        #endregion

        #region events
        public event ScrollEventHandler Scroll;
        #endregion

        #region variables
        private int _scrollY;
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
            { 
                // save the new scroll position
                _scrollY = GetVerticalScrollbarPosition();

                // raise the event
                OnScroll(new ScrollEventArgs((ScrollEventType)(m.WParam.ToInt32() & 0xffff), 0));
            }
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);

            // get the current position of the vertical scroll bar
            var scrollY = GetVerticalScrollbarPosition();

            // has the scrool position been changed by the selection?
            if (scrollY != _scrollY)
            {
                // save the new position
                _scrollY = scrollY;

                // call the event
                OnScroll(new ScrollEventArgs(ScrollEventType.EndScroll, _scrollY));
            }
        }

        private int GetVerticalScrollbarPosition()
        {
            // set-up the structure to hold the scrollbar information
            var info = new ScrollInfo();
            info.cbSize = (uint)Marshal.SizeOf(info);
            info.fMask = (int)ScrollInfoMask.SIF_ALL;

            // get the vertical scroll bar information
            var scrollY = GetScrollInfo(Handle, (int)SBOrientation.SB_VERT, ref info);
            return scrollY ? info.nPos : 0;
        }
    }
}