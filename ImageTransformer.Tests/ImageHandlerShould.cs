using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontur.ImageTransformer;
using NUnit.Framework.Internal;
using FakeItEasy;

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
    }
}
