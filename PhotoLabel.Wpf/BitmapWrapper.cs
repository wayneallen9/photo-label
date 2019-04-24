using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PhotoLabel.Wpf
{
    public class BitmapWrapper : IDisposable
    {
        #region delegates

        private delegate BitmapSource CreateBitmapSourceFromHBitmapDelegate(Bitmap bitmap);
        #endregion

        #region api
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
        #endregion

        #region variables

        private bool _disposedValue;
        private IntPtr _hBitmap;
        #endregion

        public BitmapWrapper(Bitmap bitmap)
        {
            // create the bitmap source
            BitmapSource = CreateBitmapSourceFromHBitmap(bitmap);
        }

        public BitmapSource BitmapSource { get; }

        private BitmapSource CreateBitmapSourceFromHBitmap(Bitmap bitmap)
        {
            // only create bitmap sources on UI thread
            if (Application.Current?.Dispatcher.CheckAccess() == false)
            {
                return (BitmapSource) Application.Current.Dispatcher.Invoke(
                    new CreateBitmapSourceFromHBitmapDelegate(CreateBitmapSourceFromHBitmap), DispatcherPriority.ApplicationIdle, bitmap);
            }

            // get the handle of the bitmap
            _hBitmap = bitmap.GetHbitmap();

            // now convert to a bitmap source
            return Imaging.CreateBitmapSourceFromHBitmap(_hBitmap, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            // has this already been run?
            if (_disposedValue) return;

            if (disposing)
            {
                // no managed objects to dispose
            }

            // release the GDI memory
            DeleteObject(_hBitmap);

            _disposedValue = true;
        }

        ~BitmapWrapper()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}