using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ImageTransformer.Tests.Services
{
    [TestFixture]
    public class ImageHandlerFiltersShould : ImageHandlerShould
    {
        #region Filters Tests

        [Test]
        public void ApplySepia()
        {
            var oldR = LargeBitmap.GetPixel(0, 0).R;
            var oldG = LargeBitmap.GetPixel(0, 0).G;
            var oldB = LargeBitmap.GetPixel(0, 0).B;
            int newR = (int)(oldR * 0.393f + oldG * 0.769f + oldB * 0.189f);
            int newG = (int)(oldR * 0.349f + oldG * 0.686f + oldB * 0.168f);
            int newB = (int)(oldR * 0.272f + oldG * 0.543f + oldB * 0.131f);
            newR = newR > 255 ? 255 : newR;
            newG = newG > 255 ? 255 : newG;
            newB = newB > 255 ? 255 : newB;
            ImageHandler.ApplySepia(LargeBitmap);
            Assert.AreEqual(newR, LargeBitmap.GetPixel(0, 0).R);
            Assert.AreEqual(newG, LargeBitmap.GetPixel(0, 0).G);
            Assert.AreEqual(newB, LargeBitmap.GetPixel(0, 0).B);
        }

        [Test]
        public void ApplyGrayscale()
        {
            var oldR = LargeBitmap.GetPixel(0, 0).R;
            var oldG = LargeBitmap.GetPixel(0, 0).G;
            var oldB = LargeBitmap.GetPixel(0, 0).B;

            int intensity = (oldR + oldG + oldB) / 3;
            int newR = intensity;
            int newG = intensity;
            int newB = intensity;

            ImageHandler.ApplyGrayscale(LargeBitmap);
            Assert.AreEqual(newR, LargeBitmap.GetPixel(0, 0).R);
            Assert.AreEqual(newG, LargeBitmap.GetPixel(0, 0).G);
            Assert.AreEqual(newB, LargeBitmap.GetPixel(0, 0).B);
        }

        [TestCase(0)]
        [TestCase(10)]
        [TestCase(30)]
        [TestCase(50)]
        [TestCase(90)]
        [TestCase(100)]
        public void ApplyTreshold(int level)
        {
            var oldR = LargeBitmap.GetPixel(0, 0).R;
            var oldG = LargeBitmap.GetPixel(0, 0).G;
            var oldB = LargeBitmap.GetPixel(0, 0).B;

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

            ImageHandler.ApplyThreshold(LargeBitmap, level);
            Assert.AreEqual(newR, LargeBitmap.GetPixel(0, 0).R);
            Assert.AreEqual(newG, LargeBitmap.GetPixel(0, 0).G);
            Assert.AreEqual(newB, LargeBitmap.GetPixel(0, 0).B);
        }

        [TestCase(-1)]
        [TestCase(101)]
        public void ThrowArgumentOutOfRangeExc_WhenApplyThresholdWithWrongLevel(int level)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ImageHandler.ApplyThreshold(LargeBitmap, level));
        }

        #endregion

    }
}
