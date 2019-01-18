using System.Drawing;
using System.Drawing.Imaging;

namespace PhotoLabel.Test
{
    public static class ImageUtilities
    {
        public static bool AreEqual(Bitmap actualImage, Bitmap expectedImage)
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