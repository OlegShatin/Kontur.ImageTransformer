using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ImageTransformer.Tests.Services
{
    [TestFixture]
    public class ImageHandlerTransformsShould : ImageHandlerShould
    {
        protected MemoryStream MemoryStream;
        protected Bitmap Pic;

        [SetUp]
        public void SetUp()
        {
            MemoryStream = new MemoryStream();
            File.OpenRead(ValidPicPath).CopyTo(MemoryStream);
            Pic = new Bitmap(MemoryStream);
        }

        [TearDown]
        public void TearDown()
        {
            MemoryStream.Dispose();
        }

        [Test]
        public void ChangeBitmapPixels_WhenRotateCw()
        {
            var controlPixel = Pic.GetPixel(0, Pic.Height - 1);
            var nextControlPixel = Pic.GetPixel(0, Pic.Height - 2);

            ImageHandler.RotateCw(Pic);

            Assert.AreEqual(controlPixel, Pic.GetPixel(0, 0));
            Assert.AreEqual(nextControlPixel, Pic.GetPixel(1, 0));
        }

        [Test]
        public void ChangeBitmapPixels_WhenRotateCww()
        {
            var controlPixel = Pic.GetPixel(Pic.Width - 1, 0);
            var nextControlPixel = Pic.GetPixel(Pic.Width - 1, 1);

            ImageHandler.RotateCww(Pic);

            Assert.AreEqual(controlPixel, Pic.GetPixel(0, 0));
            Assert.AreEqual(nextControlPixel, Pic.GetPixel(1, 0));
        }

        [Test]
        public void ChangeBitmapPixels_WhenFlipV()
        {
            var controlPixel = Pic.GetPixel(0, 0);
            var nextControlPixel = Pic.GetPixel(0, 1);

            ImageHandler.FlipVertical(Pic);

            Assert.AreEqual(controlPixel, Pic.GetPixel(0, Pic.Height - 1));
            Assert.AreEqual(nextControlPixel, Pic.GetPixel(0, Pic.Height - 2));
        }
    }
}