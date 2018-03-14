using System;
using System.Drawing;
using System.IO;
using Kontur.ImageTransformer.Services;
using NUnit.Framework;

namespace ImageTransformer.Tests.Services
{
    [TestFixture]
    public class ImageHandlerShould : TestWithImage
    {
        protected readonly ImageHandler ImageHandler = new ImageHandler();
        protected readonly Bitmap LargeBitmap = new Bitmap(LargePicPath);
        protected const int LargeBitmapSideSize = 512;

        [TestCase(0, 0, 10, 10)]
        [TestCase(10, 10, 100, 100)]
        [TestCase(100, 200, 50, 60)]
        public void CropImage_WhenRegionIntoSource_AndAllParamsPositive(int x, int y, int width, int height)
        {
            var result = ImageHandler.TryCropImage(LargeBitmap, out Bitmap segment, x, y, width, height);
            Assert.True(result);
            Assert.True(segment.Height == height && segment.Width == width);
            Assert.AreEqual(LargeBitmap.GetPixel(x, y), segment.GetPixel(0, 0));
            Assert.AreEqual(LargeBitmap.GetPixel(x + width - 1, y + height - 1),
                segment.GetPixel(width - 1, height - 1));
        }

        [TestCase(-1, -1, -10, -10)]
        [TestCase(0, LargeBitmapSideSize, 100, 100)]
        [TestCase(LargeBitmapSideSize, 0, 50, 60)]
        [TestCase(LargeBitmapSideSize, LargeBitmapSideSize, 1, 1)]
        [TestCase(0, -1, 10, -10)]
        [TestCase(-1, 0, -10, 10)]
        public void FailToCrop_WhenNoIntersections(int x, int y, int width, int height)
        {
            var result = ImageHandler.TryCropImage(LargeBitmap, out Bitmap segment, x, y, width, height);
            Assert.False(result);
        }

        [TestCase(10, 10, -10, -10)]
        [TestCase(100, 200, -150, -100)]
        public void CropImage_WhenWidthAndHeightAreNegative(int x, int y, int width, int height)
        {
            var succesed = ImageHandler.TryCropImage(LargeBitmap, out Bitmap result, x, y, width, height);
            Assert.True(succesed);
            var firstX = x + width > 0 ? x + width : 0;
            var firstY = y + height > 0 ? y + height : 0;
            Assert.AreEqual(LargeBitmap.GetPixel(firstX, firstY), result.GetPixel(0, 0));
        }

        

        [Test]
        public void ChangeBitmapPixels_WhenFlipH()
        {
            using (var ms = new MemoryStream())
            {
                File.OpenRead(ValidPicPath).CopyTo(ms);
                var pic = new Bitmap(ms);
                var controlPixel = pic.GetPixel(0, 0);
                var nextControlPixel = pic.GetPixel(1, 0);

                ImageHandler.FlipHorizontal(pic);

                Assert.AreEqual(controlPixel, pic.GetPixel(pic.Width - 1, 0));
                Assert.AreEqual(nextControlPixel, pic.GetPixel(pic.Width - 2, 0));
            }

        }


    }
}