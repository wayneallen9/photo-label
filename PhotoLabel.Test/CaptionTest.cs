using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace PhotoLabel.Test
{
    [TestClass]
    public class CaptionTest
    {
        [TestMethod]
        public void MiddleLeftWithCaption()
        {
            Bitmap captionedImage;

            // create the mocked services
            var logService = new Mock<Services.ILogService>().Object;

            // create the image service
            var imageService = new Services.ImageService(
                new Services.ImageLoaderService(logService),
                new Services.LineWrapService(logService),
                logService);

            // create a new test image
            using (var testImage = new Bitmap(480, 640, PixelFormat.Format24bppRgb))
            {
                // add the caption
                captionedImage = imageService.Caption(testImage, "This is a test caption", Services.CaptionAlignments.MiddleLeft, "Arial", 10f, "%", true, new SolidBrush(Color.White), Color.FromArgb(127, 255, 255, 255), Services.Rotations.Zero, 0, new CancellationToken()) as Bitmap;
            }

            // load the expected result
            var expectedImage = Properties.Resources.MiddleLeftWithCaption;

            // is it the expected result?
            var equal = ImageUtilities.AreEqual(captionedImage, expectedImage);

            Assert.AreEqual(true, equal);
        }

        [TestMethod]
        public void MiddleLeftWithoutCaption()
        {
            Bitmap captionedImage;

            // create the mocked services
            var logService = new Mock<Services.ILogService>().Object;

            // create the image service
            var imageService = new Services.ImageService(
                new Services.ImageLoaderService(logService),
                new Services.LineWrapService(logService),
                logService);

            // create a new test image
            using (var testImage = new Bitmap(480, 640, PixelFormat.Format24bppRgb))
            {
                // add the caption
                captionedImage = imageService.Caption(testImage, "This is a test caption", Services.CaptionAlignments.MiddleLeft, "Arial", 10f, "%", true, new SolidBrush(Color.White), Color.Transparent, Services.Rotations.Zero, 0, new CancellationToken()) as Bitmap;
            }

            // load the expected result
            var expectedImage = Properties.Resources.MiddleLeftWithoutCaption;

            // is it the expected result?
            var equal = ImageUtilities.AreEqual(captionedImage, expectedImage);

            Assert.AreEqual(true, equal);
        }

        [TestMethod]
        public void TopLeftWithCaption()
        {
            Bitmap captionedImage;

            // create the mocked services
            var logService = new Mock<Services.ILogService>().Object;

            // create the image service
            var imageService = new Services.ImageService(
                new Services.ImageLoaderService(logService),
                new Services.LineWrapService(logService),
                logService);

            // create a new test image
            using (var testImage = new Bitmap(480, 640, PixelFormat.Format24bppRgb))
            {
                // add the caption
                captionedImage = imageService.Caption(testImage, "This is a test caption", Services.CaptionAlignments.TopLeft, "Arial", 10f, "%", true, new SolidBrush(Color.White), Color.FromArgb(127, 255, 255, 255), Services.Rotations.Zero, 0, new CancellationToken()) as Bitmap;
            }

            // load the expected result
            var expectedImage = Properties.Resources.TopLeftWithCaption;

            // is it the expected result?
            var equal = ImageUtilities.AreEqual(captionedImage, expectedImage);

            Assert.AreEqual(true, equal);
        }

        [TestMethod]
        public void TopLeftWithoutCaption()
        {
            Bitmap captionedImage;

            // create the mocked services
            var logService = new Mock<Services.ILogService>().Object;

            // create the image service
            var imageService = new Services.ImageService(
                new Services.ImageLoaderService(logService),
                new Services.LineWrapService(logService),
                logService);

            // create a new test image
            using (var testImage = new Bitmap(480, 640, PixelFormat.Format24bppRgb))
            {
                // add the caption
                captionedImage = imageService.Caption(testImage, "This is a test caption", Services.CaptionAlignments.TopLeft, "Arial", 10f, "%", true, new SolidBrush(Color.White), Color.Transparent, Services.Rotations.Zero, 0, new CancellationToken()) as Bitmap;
            }

            // load the expected result
            var expectedImage = Properties.Resources.TopLeftWithoutCaption;

            // is it the expected result?
            var equal = ImageUtilities.AreEqual(captionedImage, expectedImage);

            Assert.AreEqual(true, equal);
        }

        [TestMethod]
        public void TopCentreWithCaption()
        {
            Bitmap captionedImage;

            // create the mocked services
            var logService = new Mock<Services.ILogService>().Object;

            // create the image service
            var imageService = new Services.ImageService(
                new Services.ImageLoaderService(logService),
                new Services.LineWrapService(logService),
                logService);

            // create a new test image
            using (var testImage = new Bitmap(480, 640, PixelFormat.Format24bppRgb))
            {
                // add the caption
                captionedImage = imageService.Caption(testImage, "This is a test caption", Services.CaptionAlignments.TopCentre, "Arial", 10f, "%", true, new SolidBrush(Color.White), Color.FromArgb(127, 255, 255, 255), Services.Rotations.Zero, 0, new CancellationToken()) as Bitmap;
            }

            // load the expected result
            var expectedImage = Properties.Resources.TopCentreWithCaption;

            // is it the expected result?
            var equal = ImageUtilities.AreEqual(captionedImage, expectedImage);

            Assert.AreEqual(true, equal);
        }

        [TestMethod]
        public void TopCentreWithoutCaption()
        {
            Bitmap captionedImage;

            // create the mocked services
            var logService = new Mock<Services.ILogService>().Object;

            // create the image service
            var imageService = new Services.ImageService(
                new Services.ImageLoaderService(logService),
                new Services.LineWrapService(logService),
                logService);

            // create a new test image
            using (var testImage = new Bitmap(480, 640, PixelFormat.Format24bppRgb))
            {
                // add the caption
                captionedImage = imageService.Caption(testImage, "This is a test caption", Services.CaptionAlignments.TopCentre, "Arial", 10f, "%", true, new SolidBrush(Color.White), Color.Transparent, Services.Rotations.Zero, 0, new CancellationToken()) as Bitmap;
            }

            // load the expected result
            var expectedImage = Properties.Resources.TopCentreWithoutCaption;

            // is it the expected result?
            var equal = ImageUtilities.AreEqual(captionedImage, expectedImage);

            Assert.AreEqual(true, equal);
        }

        [TestMethod]
        public void TopRightWithCaption()
        {
            Bitmap captionedImage;

            // create the mocked services
            var logService = new Mock<Services.ILogService>().Object;

            // create the image service
            var imageService = new Services.ImageService(
                new Services.ImageLoaderService(logService),
                new Services.LineWrapService(logService),
                logService);

            // create a new test image
            using (var testImage = new Bitmap(480, 640, PixelFormat.Format24bppRgb))
            {
                // add the caption
                captionedImage = imageService.Caption(testImage, "This is a test caption", Services.CaptionAlignments.TopRight, "Arial", 10f, "%", true, new SolidBrush(Color.White), Color.FromArgb(127, 255, 255, 255), Services.Rotations.Zero, 0, new CancellationToken()) as Bitmap;
            }

            // load the expected result
            var expectedImage = Properties.Resources.TopRightWithCaption;

            // is it the expected result?
            var equal = ImageUtilities.AreEqual(captionedImage, expectedImage);

            Assert.AreEqual(true, equal);
        }

        [TestMethod]
        public void TopRightWithoutCaption()
        {
            Bitmap captionedImage;

            // create the mocked services
            var logService = new Mock<Services.ILogService>().Object;

            // create the image service
            var imageService = new Services.ImageService(
                new Services.ImageLoaderService(logService),
                new Services.LineWrapService(logService),
                logService);

            // create a new test image
            using (var testImage = new Bitmap(480, 640, PixelFormat.Format24bppRgb))
            {
                // add the caption
                captionedImage = imageService.Caption(testImage, "This is a test caption", Services.CaptionAlignments.TopRight, "Arial", 10f, "%", true, new SolidBrush(Color.White), Color.Transparent, Services.Rotations.Zero, 0, new CancellationToken()) as Bitmap;
            }

            // load the expected result
            var expectedImage = Properties.Resources.TopRightWithoutCaption;

            // is it the expected result?
            var equal = ImageUtilities.AreEqual(captionedImage, expectedImage);

            Assert.AreEqual(true, equal);
        }
    }
}
