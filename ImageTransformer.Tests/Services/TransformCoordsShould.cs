using Kontur.ImageTransformer.Services;
using NUnit.Framework;

namespace ImageTransformer.Tests.Services
{
    [TestFixture]
    public class TransformCoordsShould
    {
        [TestCase(0, 0, 10, 15, 20, 30)]
        [TestCase(-1, 0, 10, 15, 20, 30)]
        [TestCase(-70, -30, 10, 15, 50, 60)]
        [TestCase(70, 30, -10, 15, 150, 60)]
        [TestCase(70, 30, 10, -15, 50, 260)]
        [TestCase(70, 30, -160, -15, 50, 260)]
        public void ReturnToStartCoords_AfterTwoContraRotations(int x, int y, int width, int heght, int picWidth, int picHeight)
        {
            Assert.AreEqual(x, TransformCoords.ToCww.GetX(TransformCoords.ToCw.GetY(x)));
            Assert.AreEqual(y, TransformCoords.ToCww.GetY(TransformCoords.ToCw.GetX(y, picWidth), picWidth));
            Assert.AreEqual(width, TransformCoords.ToCww.GetWidth(TransformCoords.ToCw.GetHeight(width)));
            Assert.AreEqual(heght, TransformCoords.ToCww.GetHeight(TransformCoords.ToCw.GetWidth(heght)));
        }

        [TestCase(0, 0, 10, 15, 20, 30)]
        [TestCase(-1, 0, 10, 15, 20, 30)]
        [TestCase(-70, -30, 10, 15, 50, 60)]
        [TestCase(70, 30, -10, 15, 150, 60)]
        [TestCase(70, 30, 10, -15, 50, 260)]
        [TestCase(70, 30, -160, -15, 50, 260)]
        public void ReturnToStartCoords_AfterTwoContraVFlips(int x, int y, int width, int heght, int picWidth, int picHeight)
        {
            Assert.AreEqual(x, TransformCoords.ToFlipV.GetX(TransformCoords.ToFlipV.GetX(x)));
            Assert.AreEqual(y, TransformCoords.ToFlipV.GetY(TransformCoords.ToFlipV.GetY(y, picHeight), picHeight));
            Assert.AreEqual(width, TransformCoords.ToFlipV.GetWidth(TransformCoords.ToFlipV.GetWidth(width)));
            Assert.AreEqual(heght, TransformCoords.ToFlipV.GetHeight(TransformCoords.ToFlipV.GetHeight(heght)));
        }

        [TestCase(0, 0, 10, 15, 20, 30)]
        [TestCase(-1, 0, 10, 15, 20, 30)]
        [TestCase(-70, -30, 10, 15, 50, 60)]
        [TestCase(70, 30, -10, 15, 150, 60)]
        [TestCase(70, 30, 10, -15, 50, 260)]
        [TestCase(70, 30, -160, -15, 50, 260)]
        public void ReturnToStartCoords_AfterTwoContraHFlips(int x, int y, int width, int heght, int picWidth, int picHeight)
        {
            Assert.AreEqual(x, TransformCoords.ToFlipH.GetX(TransformCoords.ToFlipH.GetX(x, picWidth), picWidth));
            Assert.AreEqual(y, TransformCoords.ToFlipH.GetY(TransformCoords.ToFlipH.GetY(y)));
            Assert.AreEqual(width, TransformCoords.ToFlipH.GetWidth(TransformCoords.ToFlipH.GetWidth(width)));
            Assert.AreEqual(heght, TransformCoords.ToFlipH.GetHeight(TransformCoords.ToFlipH.GetHeight(heght)));
        }
    }
}