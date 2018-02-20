using NUnit.Framework;
using System.IO;
using System.Net;
using Kontur.ImageTransformer;

namespace ImageTransformer.Tests
{
    [TestFixture]
    public class ImageControllerShould : TestWithImage
    {
        private AsyncHttpServer server;


        [OneTimeSetUp]
        public void FirstSetUp()
        {
            server = new AsyncHttpServer();
            server.Run("http://localhost:8080/");
        }


        [Test]
        public void UploadCorrectPicGrayscale()
        {
            var req = (HttpWebRequest) HttpWebRequest.Create("http://localhost:8080/" + "process/grayscale/0,0,10,15");
            SetUpRequestCorrectly(req);
            AttachCorrectPicToRequest(req);
            var resp = req.GetResponse();
            SaveFileFromResponse(resp, "TestFileGrayscale.png");

            Assert.AreEqual(200, (int) ((HttpWebResponse) resp).StatusCode);
        }


        [Test]
        public void UploadCorrectPicThreshold()
        {
            var req = (HttpWebRequest) HttpWebRequest.Create(
                "http://localhost:8080/" + "process/threshold(50)/0,0,512,512");
            SetUpRequestCorrectly(req);
            AttachCorrectPicToRequest(req);
            var resp = req.GetResponse();
            SaveFileFromResponse(resp, "TestFileThreshold.png");

            Assert.AreEqual(200, (int) ((HttpWebResponse) resp).StatusCode);
        }

        [Test]
        public void UploadCorrectPicSepia()
        {
            var req = (HttpWebRequest) HttpWebRequest.Create("http://localhost:8080/" + "process/sepia/0,0,10,15");
            SetUpRequestCorrectly(req);
            AttachCorrectPicToRequest(req);
            var resp = req.GetResponse();
            SaveFileFromResponse(resp, "TestFileSepia.png");
            Assert.AreEqual(200, (int) ((HttpWebResponse) resp).StatusCode);
        }
        [Test]
        public void ResponseWithNoContent_WhenThereIsNoIntersections()
        {
            var req = (HttpWebRequest)HttpWebRequest.Create("http://localhost:8080/" + "process/sepia/-1,0,-10,15");
            SetUpRequestCorrectly(req);
            AttachCorrectPicToRequest(req);
            var resp = req.GetResponse();
            Assert.AreEqual(204, (int)((HttpWebResponse)resp).StatusCode);
        }

        [Test]
        public void RefuseOver100KBPic()
        {
            var req = (HttpWebRequest) HttpWebRequest.Create("http://localhost:8080/" + "process/sepia/300,30,300,100");
            SetUpRequestCorrectly(req);
            AttachTooLargePicToRequest(req);
            AssertResponseHasFailCode(req, 400);
        }


        [Test]
        public void RefuseNotPostRequest()
        {
            var req = (HttpWebRequest) HttpWebRequest.Create("http://localhost:8080/" + "process/sepia/300,30,300,100");
            req.Method = "PUT";
            req.ContentType = "application/octet-stream";
            req.KeepAlive = true;
            AttachCorrectPicToRequest(req);
            AssertResponseHasFailCode(req, 400);
        }

        [TestCase("bad/process/sepia/300,30,300,100")]
        [TestCase("process/sepia/300,30,300")]
        [TestCase("process/sepia/300,30,300,sym")]
        [TestCase("process/sepia/300;30;300;100")]
        [TestCase("bad/sepia/300,30,300,100")]
        [TestCase("sepia/300,30,300,100")]
        [TestCase("sepia/300,30,300,100/process")]
        [TestCase("300,30,300,100/sepia/process")]
        [TestCase("process/threshold(5032)/0,0,512,512")]
        [TestCase("process/threshold(5032/0,0,512,512")]
        [TestCase("process/threshold(50/0,0,512,512")]
        [TestCase("process/threshold(sym)/0,0,512,512")]
        [TestCase("process/(50)/0,0,512,512")]
        [TestCase("process/threshold50)/0,0,512,512")]
        public void RefuseWrongPathFormat(string path)
        {
            var req = (HttpWebRequest)HttpWebRequest.Create("http://localhost:8080/" + path);
            SetUpRequestCorrectly(req);
            AttachCorrectPicToRequest(req);
            AssertResponseHasFailCode(req, 400);
        }
        [OneTimeTearDown]
        public void TearDown()
        {
            server.Stop();
        }

        #region Helper methods

        private void SaveFileFromResponse(WebResponse resp, string filename)
        {
            using (var stream = resp.GetResponseStream())
            {
                stream.CopyTo(File.Create(StoragePath + filename));
            }
        }

        private void AttachCorrectPicToRequest(HttpWebRequest req)
        {
            Stream newStream = req.GetRequestStream();

            File.OpenRead(ValidPicPath).CopyTo(newStream);
        }

        private void SetUpRequestCorrectly(HttpWebRequest req)
        {
            req.Method = "POST";
            req.ContentType = "application/octet-stream";
            req.KeepAlive = true;
        }

        private void AssertResponseHasFailCode(HttpWebRequest req, int expectedFailCode)
        {
            try
            {
                using (WebResponse response = req.GetResponse())
                {
                    Assert.Fail();
                }
            }
            catch (WebException e)
            {
                using (WebResponse response = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse) response;
                    Assert.AreEqual(expectedFailCode, (int) httpResponse.StatusCode);
                }
            }
        }

        private void AttachTooLargePicToRequest(HttpWebRequest req)
        {
            Stream newStream = req.GetRequestStream();

            File.OpenRead(LargePicPath).CopyTo(newStream);
        }

        #endregion
    }
}