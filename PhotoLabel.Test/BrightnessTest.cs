using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace PhotoLabel.Test
{
    [TestClass]
    public class BrightnessTest
    {
        [TestMethod]
        public void IncreaseBrightness()
        {
            Bitmap captionedImage;

            var logService = new Mock<Services.ILogService>().Object;
            var imageLoaderService = new Services.ImageLoaderService(logService);
            var lineWrapService = new Services.LineWrapService(logService);
            var imageService = new Services.ImageService(imageLoaderService, lineWrapService, logService);

            // create a new test image
            using (var testImage = new Bitmap(480, 640, PixelFormat.Format24bppRgb))
            {
                // add the caption
                captionedImage = imageService.Caption(testImage, "This is a test caption", Services.CaptionAlignments.MiddleLeft, "Arial", 10f, "%", true, new SolidBrush(Color.White), Color.FromArgb(127, 255, 255, 255), Services.Rotations.Zero, 50, new CancellationToken()) as Bitmap;
            }

            // load the expected result
            var expectedImage = Properties.Resources.MiddleLeftWithCaption;

            // is it the expected result?
            var equal = ImageUtilities.AreEqual(captionedImage, expectedImage);

            Assert.AreEqual(true, equal);
        }
    }
}
