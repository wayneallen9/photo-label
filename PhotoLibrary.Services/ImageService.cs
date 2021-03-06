﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Media.Imaging;
using PhotoLabel.Services.Models;
using Shared;
using Shared.Attributes;
using Encoder = System.Drawing.Imaging.Encoder;

namespace PhotoLabel.Services
{
    [Singleton]  
    public class ImageService : IImageService
    {
        #region constants

        private const string LockedPattern =
            @"^The process cannot access the file '.+' because it is being used by another process\.$";
        #endregion

        #region variables

        private readonly IConfigurationService _configurationService;
        private readonly IImageCaptionServiceFactory _imageCaptionServiceFactory;
        private readonly ILogger _logger;
        private readonly string _shortDateFormat;
        #endregion

        public ImageService(
            IConfigurationService configurationService,
            IImageCaptionServiceFactory imageCaptionServiceFactory,
            ILogger logger)
        {
            // save the dependency injections
            _configurationService = configurationService;
            _imageCaptionServiceFactory = imageCaptionServiceFactory;
            _logger = logger;

            // create the format for the date
            _shortDateFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("yyyy", "yy");
        }

        public Bitmap Brightness(Image source, int brightness)
        {
            using (var logger = _logger.Block()) {
                var brightnessAmount = brightness / 100.0f;

                logger.Trace($"Creating image {source.Width}px x {source.Height}px...");
                var image = new Bitmap(source.Width, source.Height, source.PixelFormat);

                logger.Trace("Creating matrix to adjust colour...");
                var adjustArray = new[]
                {
                    new[] { 1.0f, 0, 0, 0, 0},
                    new[] { 0, 1.0f, 0, 0, 0},
                    new[] { 0, 0, 1.0f, 0, 0},
                    new[] { 0, 0, 0, 1.0f, 0},
                    new [] { brightnessAmount, brightnessAmount, brightnessAmount, 0, 1}
                };

                logger.Trace("Creating the image attributes...");
                var imageAttributes = new ImageAttributes();
                imageAttributes.ClearColorMatrix();
                imageAttributes.SetColorMatrix(new ColorMatrix(adjustArray), ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                logger.Trace("Copying source image onto copy...");
                using (var imageGraphics = Graphics.FromImage(image))
                {
                    logger.Trace("Setting up graphics manager...");
                    imageGraphics.SmoothingMode = SmoothingMode.HighQuality;
                    imageGraphics.CompositingQuality = CompositingQuality.HighQuality;
                    imageGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    imageGraphics.DrawImage(source, new Rectangle(0, 0, image.Width, image.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
                }

                return image;
            
            }
        }

        public Bitmap Get(string filename, int width, int height)
        {
            using (var logger = _logger.Block()) {
                while (true)
                {
                    try
                    {
                        logger.Trace($@"Opening ""{filename}""...");
                        using (var fileStream =
                            new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            logger.Trace($"Getting \"{filename}\"...");
                            using (var image = (Bitmap) Image.FromStream(fileStream))
                            {
                                return Resize(image, width, height);
                            }
                        }
                    }
                    catch (IOException ex)
                    {
                        if (!Regex.IsMatch(ex.Message, LockedPattern))
                            throw;

                        // the file may still be locked - pause and then try again
                        Thread.Sleep(500);
                    }
                }
            }
        }

        private ImageCodecInfo GetEncoder(System.Drawing.Imaging.ImageFormat imageFormat)
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Getting image decoders...");
                var codecs = ImageCodecInfo.GetImageDecoders();

                logger.Trace($"Searching {codecs.Length} codecs for a match...");
                return codecs.FirstOrDefault(c => c.FormatID == imageFormat.Guid);
            
            }
        }

        public ExifData GetExifData(string filename)
        {
            using (var logger = _logger.Block()) {
                while (true)
                {
                    try
                    {
                        // create the object to return
                        var exifData = new ExifData();

                        logger.Trace($@"Opening ""{filename}""...");
                        using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            var bitmapSource = BitmapFrame.Create(fs, BitmapCreateOptions.DelayCreation,
                                BitmapCacheOption.None);
                            if (!(bitmapSource.Metadata is BitmapMetadata bitmapMetadata)) return exifData;

                            // try and parse the date
                            exifData.DateTaken = !DateTime.TryParse(bitmapMetadata.DateTaken, out var dateTaken)
                                ? bitmapMetadata.DateTaken
                                : dateTaken.Date.ToString(_shortDateFormat);

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
                    catch (IOException ex)
                    {
                        // check that this is a locked message
                        if (!Regex.IsMatch(ex.Message, LockedPattern, RegexOptions.IgnoreCase)) throw;

                        // give the file time to be released
                        Thread.Sleep(500);
                    }
                }
            }
        }

        public string GetFilename(string outputDirectory, string imagePath, ImageFormat imageFormat)
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Getting output file name...");
                var imageFilename = Path.GetFileName(imagePath) ??
                                    throw new NullReferenceException(
                                        $@"The path of the file ""{imagePath}"" is null.");

                return $"{Path.Combine(outputDirectory, imageFilename)}.{imageFormat.ToString().ToLower()}";
            }
        }

        private float? GetLatitude(BitmapMetadata bitmapMetadata)
        {
            using (var logger = _logger.Block()) {
                try
                {
                    logger.Trace("Checking if file has latitude information...");
                    if (!(bitmapMetadata.GetQuery("System.GPS.Latitude.Proxy") is string latitudeRef))
                    {
                        logger.Trace("File does not have latitude information.  Exiting...");
                        return null;
                    }

                    logger.Trace($"Parsing latitude information \"{latitudeRef}\"...");
                    var latitudeMatch = Regex.Match(latitudeRef, @"^(\d+),([0123456789.]+)([SN])");
                    if (!latitudeMatch.Success)
                    {
                        logger.Trace($"Unable to parse \"{latitudeRef}\".  Exiting...");
                        return null;
                    }

                    logger.Trace("Converting to a float...");
                    var latitudeDecimal = float.Parse(latitudeMatch.Groups[1].Value) +
                                          float.Parse(latitudeMatch.Groups[2].Value) / 60;
                    if (latitudeMatch.Groups[3].Value == "S") latitudeDecimal *= -1;

                    return latitudeDecimal;
                }
                catch (NotSupportedException)
                {
                    logger.Trace("Unable to query for latitude.  Returning...");
                    return null;

                }
            }
        }

        private float? GetLongitude(BitmapMetadata bitmapMetadata)
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Checking if file has longitude information...");
                if (!(bitmapMetadata.GetQuery("System.GPS.Longitude.Proxy") is string longitudeRef))
                {
                    logger.Trace("File does not have longitude information.  Exiting...");
                    return null;
                }

                logger.Trace($"Parsing longitude information \"{longitudeRef}\"...");
                var longitudeMatch = Regex.Match(longitudeRef, @"^(\d+),([0123456789.]+)([EW])");
                if (!longitudeMatch.Success)
                {
                    logger.Trace($"Unable to parse \"{longitudeRef}\".  Exiting...");
                    return null;
                }

                logger.Trace("Converting to a float...");
                var longitudeDecimal = float.Parse(longitudeMatch.Groups[1].Value) + float.Parse(longitudeMatch.Groups[2].Value) / 60;
                if (longitudeMatch.Groups[3].Value == "W") longitudeDecimal *= -1;

                return longitudeDecimal;
            
            }
        }

        public Bitmap Caption(Bitmap original, string caption, bool appendDateTakenToCaption, string dateTaken,
            Rotations rotation, CaptionAlignments captionAlignment, string fontName, float fontSize, string fontType,
            bool fontBold, Brush brush, Color backgroundColour, bool useCanvas, int? canvasWidth, int? canvasHeight, CancellationToken cancellationToken)
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Checking if image has a canvas...");
                var imageCaptionService = _imageCaptionServiceFactory.Create(useCanvas, canvasWidth, canvasHeight);

                logger.Trace($@"Adding the caption ""{caption}""...");
                return imageCaptionService.Caption(original, caption, appendDateTakenToCaption, dateTaken, rotation, captionAlignment, fontName, fontSize, fontType, fontBold, brush, backgroundColour, cancellationToken);
            }
        }

        public Bitmap Overlay(Bitmap image, Image overlay, int x, int y)
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Getting graphics manager for new image...");
                using (var graphics = Graphics.FromImage(image))
                {
                    logger.Trace("Setting up graphics manager...");
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    logger.Trace("Drawing overlay...");
                    graphics.DrawImage(overlay, new Point(x, y));
                }

                return image;
            
            }
        }

        public Stream ReduceQuality(Bitmap image, long quality)
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Getting JPEG encoder...");
                var jpegEncoder = GetEncoder(System.Drawing.Imaging.ImageFormat.Jpeg) ??
                                  throw new InvalidOperationException("Cannot find Jpeg image decoder codec");

                logger.Trace("Creating encoder quality parameter...");
                var qualityEncoder = Encoder.Quality;
                var encoderParameters = new EncoderParameters(1)
                {
                    Param = {[0] = new EncoderParameter(qualityEncoder, quality)}
                };

                logger.Trace("Creating stream to return...");
                var memoryStream = new MemoryStream();

                logger.Trace($"Reducing image quality to {quality}...");
                image.Save(memoryStream, jpegEncoder, encoderParameters);

                logger.Trace($"Resetting memory position to 0 of {memoryStream.Length}");
                memoryStream.Position = 0;

                return memoryStream;
            
            }
        }

        public Bitmap Resize(Bitmap image, int width, int height)
        {
            using (var logger = _logger.Block()) {
                logger.Trace($"Resizing to fit {width}px x{height}px canvas...");
                var canvas = new Bitmap(width, height);

                logger.Trace("Calculating size of resized image...");
                var aspectRatio = Math.Min(width / (float)image.Width, height / (float)image.Height);
                var newWidth = image.Width * aspectRatio;
                var newHeight = image.Height * aspectRatio;
                var newX = (width - newWidth) / 2;
                var newY = (height - newHeight) / 2;

                logger.Trace("Drawing resized image...");
                using (var graphics = Graphics.FromImage(canvas))
                {
                    // initialise the pen
                    graphics.SmoothingMode = SmoothingMode.HighSpeed;
                    graphics.CompositingQuality = CompositingQuality.HighSpeed;
                    graphics.InterpolationMode = InterpolationMode.Low;

                    // draw the black background
                    graphics.FillRectangle(new SolidBrush(Color.Black), 0, 0, width, height);

                    // now draw the image over the top
                    graphics.DrawImage(image, newX, newY, newWidth, newHeight);
                }

                return canvas;
            
            }
        }

        public List<string> Find(string folderPath)
        {
            using (var logger = _logger.Block()) {
                logger.Trace($@"Getting the image files in ""{folderPath}""...");
                return Directory.EnumerateFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(s =>
                        (
                            s.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase) ||
                            s.EndsWith(".jpeg", StringComparison.CurrentCultureIgnoreCase) ||
                            s.EndsWith(".gif", StringComparison.CurrentCultureIgnoreCase) ||
                            s.EndsWith(".bmp", StringComparison.CurrentCultureIgnoreCase) ||
                            s.EndsWith(".png", StringComparison.CurrentCultureIgnoreCase) ||
                            s.EndsWith(".tif", StringComparison.CurrentCultureIgnoreCase)
                        ) &&
                        (File.GetAttributes(s) & FileAttributes.Hidden) == 0)
                    .ToList();
            }
        }

        public void Save(Bitmap image, string filename, ImageFormat imageFormat)
        {
            using (var logger = _logger.Block()) {
                logger.Trace("Getting format to save...");
                System.Drawing.Imaging.ImageFormat imagingImageFormat;
                switch (imageFormat)
                {
                    case ImageFormat.Bmp:
                        imagingImageFormat = System.Drawing.Imaging.ImageFormat.Bmp;

                        break;
                    case ImageFormat.Jpeg:
                        imagingImageFormat = System.Drawing.Imaging.ImageFormat.Jpeg;

                        break;
                    default:
                        imagingImageFormat = System.Drawing.Imaging.ImageFormat.Png;

                        break;
                }

                logger.Trace($@"Creating ""{filename}""...");
                using (var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    logger.Trace("Checking if there is a size limitation...");
                    if (_configurationService.MaxImageSize != null)
                    {
                        logger.Trace("Reducing image to fit size limitation...");
                        var imageReducer = ImageReducerFactory.Create(imageFormat);
                        using (var imageStream = imageReducer.Reduce(image))
                        {
                            logger.Trace("Saving reduced image to disk...");
                            imageStream.CopyTo(fileStream);
                        }
                    }
                    else
                    {
                        logger.Trace($@"Saving image to ""{filename}""...");
                        image.Save(fileStream, imagingImageFormat);
                    }
                }
            
            }
        }
    }
}