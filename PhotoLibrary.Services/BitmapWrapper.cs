using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Media.Imaging;
using PhotoLabel.DependencyInjection;
using PhotoLabel.Services.Models;

namespace PhotoLabel.Services
{
    public class BitmapWrapper : IDisposable
    {
        public BitmapWrapper(
            string path)
        {
            // save dependencies
            _path = path;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                _fileStream?.Dispose();
                _bitmap?.Dispose();
                _preview?.Dispose();
            }

            _disposedValue = true;
        }

        public Bitmap GetBitmap()
        {
            // get dependencies
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace($@"Checking if bitmap for ""{_path}"" has already been cached...");
                if (_bitmap != null)
                {
                    logService.Trace($@"Bitmap for ""{_path}"" has been cached.  Returning...");
                    return _bitmap;
                }

                logService.Trace($@"Loading ""{_path}"" from disk...");
                _bitmap = (Bitmap) Image.FromStream(GetFileStream());

                logService.Trace($@"Returning a copy of ""{_path}""...");
                return new Bitmap(_bitmap);
            }
            finally
            {
                logService.TraceExit();
            }
        }

        public ExifData GetExifData()
        {
            // create dependencies
            var logService = NinjectKernel.Get<ILogService>();

            // create variables
            var stopWatch = Stopwatch.StartNew();

            logService.TraceEnter();
            try
            {
                // create the object to return
                var exifData = new Models.ExifData();

                using (var fs = GetFileStream())
                {
                    var bitmapSource =
                        BitmapFrame.Create(fs, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    if (!(bitmapSource.Metadata is BitmapMetadata bitmapMetadata)) return exifData;

                    // try and parse the date
                    exifData.DateTaken = !DateTime.TryParse(bitmapMetadata.DateTaken, out var dateTaken)
                        ? bitmapMetadata.DateTaken
                        : dateTaken.Date.ToString(ShortDateFormat);

                    // is there a latitude on the image
                    exifData.Latitude = GetLatitude(bitmapMetadata);
                    if (exifData.Latitude.HasValue) exifData.Longitude = GetLongitude(bitmapMetadata);

                    try
                    {
                        // is there a title on the image?
                        exifData.Title = bitmapMetadata.Title;
                    }
                    catch (NotSupportedException)
                    {
                        // ignore this exception
                    }
                }

                return exifData;
            }
            finally
            {
                logService.TraceExit(stopWatch);
            }
        }

        private Stream GetFileStream()
        {
            // create dependencies
            var logService = NinjectKernel.Get<ILogService>();

            // create variables
            var stopWatch = Stopwatch.StartNew();

            logService.TraceEnter();
            try
            {
                logService.Trace($@"Checking if file stream for ""{_path}"" has already been cached...");
                if (_fileStream != null)
                {
                    logService.Trace($@"File stream for ""{_path}"" has been cached.  Returning...");
                    _fileStream.Position = 0;
                    return _fileStream;
                }

                var count = 0;
                while (true)
                {
                    try
                    {
                        logService.Trace($@"Trying to open ""{_path}"".  Count = {count}...");
                        _fileStream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);

                        return _fileStream;
                    }
                    catch (IOException ex)
                    {
                        if (count++ == 30) throw;

                        Thread.Sleep(1000);
                    }
                }
            }
            finally
            {
                logService.TraceExit(stopWatch);
            }
        }

        private static float? GetLatitude(BitmapMetadata bitmapMetadata)
        {
            // create dependencies
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace("Checking if file has latitude information...");
                if (!(bitmapMetadata.GetQuery("System.GPS.Latitude.Proxy") is string latitudeRef))
                {
                    logService.Trace("File does not have latitude information.  Exiting...");
                    return null;
                }

                logService.Trace($"Parsing latitude information \"{latitudeRef}\"...");
                var latitudeMatch = Regex.Match(latitudeRef, @"^(\d+),([0123456789.]+)([SN])");
                if (!latitudeMatch.Success)
                {
                    logService.Trace($"Unable to parse \"{latitudeRef}\".  Exiting...");
                    return null;
                }

                logService.Trace("Converting to a float...");
                var latitudeDecimal = float.Parse(latitudeMatch.Groups[1].Value) +
                                      float.Parse(latitudeMatch.Groups[2].Value) / 60;
                if (latitudeMatch.Groups[3].Value == "S") latitudeDecimal *= -1;

                return latitudeDecimal;
            }
            catch (NotSupportedException)
            {
                logService.Trace("Unable to query for latitude.  Returning...");
                return null;
            }
            finally
            {
                logService.TraceExit();
            }
        }

        private static float? GetLongitude(BitmapMetadata bitmapMetadata)
        {
            // create dependencies
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace("Checking if file has longitude information...");
                if (!(bitmapMetadata.GetQuery("System.GPS.Longitude.Proxy") is string longitudeRef))
                {
                    logService.Trace("File does not have longitude information.  Exiting...");
                    return null;
                }

                logService.Trace($"Parsing longitude information \"{longitudeRef}\"...");
                var longitudeMatch = Regex.Match(longitudeRef, @"^(\d+),([0123456789.]+)([EW])");
                if (!longitudeMatch.Success)
                {
                    logService.Trace($"Unable to parse \"{longitudeRef}\".  Exiting...");
                    return null;
                }

                logService.Trace("Converting to a float...");
                var longitudeDecimal = float.Parse(longitudeMatch.Groups[1].Value) + float.Parse(longitudeMatch.Groups[2].Value) / 60;
                if (longitudeMatch.Groups[3].Value == "W") longitudeDecimal *= -1;

                return longitudeDecimal;
            }
            finally
            {
                logService.TraceExit();
            }
        }

        public Bitmap GetPreview()
        {
            // get dependencies
            var logService = NinjectKernel.Get<ILogService>();

            logService.TraceEnter();
            try
            {
                logService.Trace($@"Checking if preview for ""{_path}"" has already been cached...");
                if (_preview != null)
                {
                    logService.Trace($@"Bitmap for ""{_path}"" has been cached.  Returning...");
                    return _preview;
                }

                logService.Trace($@"Getting bitmap for ""{_path}""...");
                var bitmap = GetBitmap();

                logService.Trace($"Resizing to fit {PreviewWidth}px x{PreviewHeight}px canvas...");
                _preview = new Bitmap(PreviewWidth, PreviewHeight);

                logService.Trace("Calculating size of resized image...");
                var aspectRatio = Math.Min(PreviewWidth / (float)bitmap.Width, PreviewHeight / (float)bitmap.Height);
                var newPreviewWidth = bitmap.Width * aspectRatio;
                var newPreviewHeight = bitmap.Height * aspectRatio;
                var newX = (PreviewWidth - newPreviewWidth) / 2;
                var newY = (PreviewHeight - newPreviewHeight) / 2;

                logService.Trace("Drawing resized image...");
                using (var graphics = Graphics.FromImage(_preview))
                {
                    // initialise the pen
                    graphics.SmoothingMode = SmoothingMode.HighSpeed;
                    graphics.CompositingQuality = CompositingQuality.HighSpeed;
                    graphics.InterpolationMode = InterpolationMode.Low;

                    // draw the black background
                    graphics.FillRectangle(new SolidBrush(Color.Black), 0, 0, PreviewWidth, PreviewHeight);

                    // now draw the image over the top
                    graphics.DrawImage(bitmap, newX, newY, newPreviewWidth, newPreviewHeight);
                }


                return _bitmap;
            }
            finally
            {
                logService.TraceExit();
            }
        }

        #region constants
        private const int PreviewHeight = 128;
        private const int PreviewWidth = 128;
        #endregion

        #region variables
        private static readonly string ShortDateFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("yyyy", "yy");

        private Bitmap _bitmap;
        private bool _disposedValue; // To detect redundant calls
        private Stream _fileStream;
        private readonly string _path;
        private Bitmap _preview;
        #endregion

        #region IDisposable
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
