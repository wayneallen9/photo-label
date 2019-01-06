﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Drawing;
using System.Drawing.Imaging;

namespace PhotoLabel.Test
{
    [TestClass]
    public class CaptionTest
    {
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
                captionedImage = imageService.Caption(testImage, "This is a test caption", Services.CaptionAlignments.TopLeft, "Arial", 10f, "%", true, new SolidBrush(Color.White), Color.FromArgb(127, 255, 255, 255), Services.Rotations.Zero) as Bitmap;
            }

            // load the expected result
            var expectedImage = Properties.Resources.TopLeftWithCaption;

            // is it the expected result?
            var equal = AreEqual(captionedImage, expectedImage);

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
                captionedImage = imageService.Caption(testImage, "This is a test caption", Services.CaptionAlignments.TopLeft, "Arial", 10f, "%", true, new SolidBrush(Color.White), Color.Transparent, Services.Rotations.Zero) as Bitmap;
            }

            // load the expected result
            var expectedImage = Properties.Resources.TopLeftWithoutCaption;

            // is it the expected result?
            var equal = AreEqual(captionedImage, expectedImage);

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
                captionedImage = imageService.Caption(testImage, "This is a test caption", Services.CaptionAlignments.TopCentre, "Arial", 10f, "%", true, new SolidBrush(Color.White), Color.FromArgb(127, 255, 255, 255), Services.Rotations.Zero) as Bitmap;
            }

            // load the expected result
            var expectedImage = Properties.Resources.TopCentreWithCaption;

            // is it the expected result?
            var equal = AreEqual(captionedImage, expectedImage);

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
                captionedImage = imageService.Caption(testImage, "This is a test caption", Services.CaptionAlignments.TopCentre, "Arial", 10f, "%", true, new SolidBrush(Color.White), Color.Transparent, Services.Rotations.Zero) as Bitmap;
            }

            // load the expected result
            var expectedImage = Properties.Resources.TopCentreWithoutCaption;

            // is it the expected result?
            var equal = AreEqual(captionedImage, expectedImage);

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
                captionedImage = imageService.Caption(testImage, "This is a test caption", Services.CaptionAlignments.TopRight, "Arial", 10f, "%", true, new SolidBrush(Color.White), Color.FromArgb(127, 255, 255, 255), Services.Rotations.Zero) as Bitmap;
            }

            // load the expected result
            var expectedImage = Properties.Resources.TopRightWithCaption;

            // is it the expected result?
            var equal = AreEqual(captionedImage, expectedImage);

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
                captionedImage = imageService.Caption(testImage, "This is a test caption", Services.CaptionAlignments.TopRight, "Arial", 10f, "%", true, new SolidBrush(Color.White), Color.Transparent, Services.Rotations.Zero) as Bitmap;
            }

            // load the expected result
            var expectedImage = Properties.Resources.TopRightWithoutCaption;

            // is it the expected result?
            var equal = AreEqual(captionedImage, expectedImage);

            Assert.AreEqual(true, equal);
        }
        private static bool AreEqual(Bitmap actualImage, Bitmap expectedImage)
        {
            // are they the same dimension?
            if (actualImage.Size != expectedImage.Size) return false;

            // get the size of the images
            var rect = new Rectangle(0, 0, actualImage.Width, actualImage.Height);

            // get the bits the image is composed of
            var actualImageBits = actualImage.LockBits(rect, ImageLockMode.ReadOnly, actualImage.PixelFormat);
            try
            {
                var expectedImageBits = expectedImage.LockBits(rect, ImageLockMode.ReadOnly, expectedImage.PixelFormat);
                try
                {
                    unsafe
                    {
                        byte* ptr1 = (byte*)actualImageBits.Scan0.ToPointer();
                        byte* ptr2 = (byte*)expectedImageBits.Scan0.ToPointer();
                        int width = rect.Width * 3; // for 24bpp pixel data
                        for (int y = 0; y < rect.Height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                if (*ptr1 != *ptr2)
                                {
                                    return false;
                                }
                                ptr1++;
                                ptr2++;
                            }
                            ptr1 += actualImageBits.Stride - width;
                            ptr2 += expectedImageBits.Stride - width;
                        }
                    }
                }
                finally
                {

                    expectedImage.UnlockBits(expectedImageBits);
                }
            }
            finally
            {
                actualImage.UnlockBits(actualImageBits);
            }

            return true;
        }
    }
}