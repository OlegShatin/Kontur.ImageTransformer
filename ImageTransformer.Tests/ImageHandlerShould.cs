using NUnit.Framework;
using System;
using System.Drawing;
using Kontur.ImageTransformer.Services;

namespace ImageTransformer.Tests
{
    [TestFixture]
    public class ImageHandlerShould : TestWithImage
    {
        private readonly ImageHandler imageHandler = new ImageHandler();
        private readonly Bitmap largeBitmap = new Bitmap(LargePicPath);
        private const int LargeBitmapSideSize = 512;
        [TestCase(0,0,10,10)]
        [TestCase(10, 10, 100, 100)]
        [TestCase(100, 200, 50, 60)]
        public void CropImage_WhenRegionIntoSource_AndAllParamsPositive(int x, int y,int width,int  height)
        {
            var result = imageHandler.TryCropImage(largeBitmap, out Bitmap segment, x, y, width, height);
            Assert.True(result);
            Assert.True(segment.Height == height && segment.Width == width);
            Assert.AreEqual(largeBitmap.GetPixel(x, y), segment.GetPixel(0, 0));
            Assert.AreEqual(largeBitmap.GetPixel(x + width - 1, y + height - 1), segment.GetPixel(width - 1, height - 1));
        }

        [TestCase(-1, -1, -10, -10)]
        [TestCase(0, LargeBitmapSideSize, 100, 100)]
        [TestCase(LargeBitmapSideSize, 0, 50, 60)]
        [TestCase(LargeBitmapSideSize, LargeBitmapSideSize, 1, 1)]
        [TestCase(0, -1, 10, -10)]
        [TestCase(-1, 0, -10, 10)]
        public void FailToCrop_WhenNoIntersections(int x, int y, int width, int height)
        {
            var result = imageHandler.TryCropImage(largeBitmap, out Bitmap segment, x, y, width, height);
            Assert.False(result);
        }

        [TestCase(10, 10, -10, -10)]
        [TestCase(100, 200, -150, -100)]
        public void CropImage_WhenWidthAndHeightAreNegative(int x, int y, int width, int height)
        {
            var succesed = imageHandler.TryCropImage(largeBitmap, out Bitmap result, x, y, width, height);
            Assert.True(succesed);
            var firstX = x + width > 0 ? x + width : 0;
            var firstY = y + height > 0 ? y + height : 0;
            Assert.AreEqual(largeBitmap.GetPixel(firstX, firstY), result.GetPixel(0, 0));
        }
        [Test]
        public void ApplySepia()
        {
            var oldR = largeBitmap.GetPixel(0, 0).R;
            var oldG = largeBitmap.GetPixel(0, 0).G;
            var oldB = largeBitmap.GetPixel(0, 0).B;
            int newR = (int)(oldR * 0.393f + oldG * 0.769f + oldB * 0.189f);
            int newG = (int)(oldR * 0.349f + oldG * 0.686f + oldB * 0.168f);
            int newB = (int)(oldR * 0.272f + oldG * 0.543f + oldB * 0.131f);
            newR = newR > 255 ? 255 : newR;
            newG = newG > 255 ? 255 : newG;
            newB = newB > 255 ? 255 : newB;
            imageHandler.ApplySepia(largeBitmap);
            Assert.AreEqual(newR, largeBitmap.GetPixel(0,0).R);
            Assert.AreEqual(newG, largeBitmap.GetPixel(0, 0).G);
            Assert.AreEqual(newB, largeBitmap.GetPixel(0, 0).B);
        }

        [Test]
        public void ApplyGrayscale()
        {
            var oldR = largeBitmap.GetPixel(0, 0).R;
            var oldG = largeBitmap.GetPixel(0, 0).G;
            var oldB = largeBitmap.GetPixel(0, 0).B;

            int intensity = (oldR + oldG + oldB) / 3;
            int newR = intensity;
            int newG = intensity;
            int newB = intensity;

            imageHandler.ApplyGrayscale(largeBitmap);
            Assert.AreEqual(newR, largeBitmap.GetPixel(0, 0).R);
            Assert.AreEqual(newG, largeBitmap.GetPixel(0, 0).G);
            Assert.AreEqual(newB, largeBitmap.GetPixel(0, 0).B);
        }
        [TestCase(0)]
        [TestCase(10)]
        [TestCase(30)]
        [TestCase(50)]
        [TestCase(90)]
        [TestCase(100)]
        public void ApplyTreshold(int level)
        {
            var oldR = largeBitmap.GetPixel(0, 0).R;
            var oldG = largeBitmap.GetPixel(0, 0).G;
            var oldB = largeBitmap.GetPixel(0, 0).B;

            int intensity = (oldR + oldG + oldB) / 3;
            int newR;
            int newG;
            int newB;
            if (intensity >= 255 * level / 100)
            {
                newR = 255;
                newG = 255;
                newB = 255;
            }
            else
            {
                newR = 0;
                newG = 0;
                newB = 0;
            }

            imageHandler.ApplyThreshold(largeBitmap, level);
            Assert.AreEqual(newR, largeBitmap.GetPixel(0, 0).R);
            Assert.AreEqual(newG, largeBitmap.GetPixel(0, 0).G);
            Assert.AreEqual(newB, largeBitmap.GetPixel(0, 0).B);
        }

        [TestCase(-1)]
        [TestCase(101)]
        public void ThrowArgumentOutOfRangeExc_WhenApplyThresholdWithWrongLevel(int level)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => imageHandler.ApplyThreshold(largeBitmap, level));
        }
    }
    
}
