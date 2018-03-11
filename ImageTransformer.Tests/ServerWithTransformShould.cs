using NUnit.Framework;
using System.IO;
using System.Net;
using Kontur.ImageTransformer;

namespace ImageTransformer.Tests
{
    [TestFixture]
    public class ServerWithTransformShould : TestWithImage
    {
        private AsyncHttpServer server;
        private const string Host = "http://localhost:8080/";

        [OneTimeSetUp]
        public void FirstSetUp()
        {
            server = new AsyncHttpServer();
            server.Start(Host);
        }


        [Test]
        public void UploadCorrectPic_AndRotateCw()
        {
            var req = (HttpWebRequest) HttpWebRequest.Create(Host + "process/rotate-cw/125,0,125,170");
            SetUpRequestCorrectly(req);
            AttachCorrectPicToRequest(req);
            var resp = req.GetResponse();
            SaveFileFromResponse(resp, "TestFileRotateCw.png");

            Assert.AreEqual(200, (int) ((HttpWebResponse) resp).StatusCode);
        }


        [Test]
        public void UploadCorrectPic_AndRotateCww()
        {
            var req = (HttpWebRequest) HttpWebRequest.Create(
                Host + "process/rotate-cw/125,0,125,170");
            SetUpRequestCorrectly(req);
            AttachCorrectPicToRequest(req);
            var resp = req.GetResponse();
            SaveFileFromResponse(resp, "TestFileRotateCww.png");

            Assert.AreEqual(200, (int) ((HttpWebResponse) resp).StatusCode);
        }

        [Test]
        public void UploadCorrectPic_AndFlipV()
        {
            var req = (HttpWebRequest) HttpWebRequest.Create(Host + "process/flip-v/125,0,125,170");
            SetUpRequestCorrectly(req);
            AttachCorrectPicToRequest(req);
            var resp = req.GetResponse();
            SaveFileFromResponse(resp, "TestFileSepia.png");
            Assert.AreEqual(200, (int) ((HttpWebResponse) resp).StatusCode);
        }

        [Test]
        public void UploadCorrectPic_AndFlipH()
        {
            var req = (HttpWebRequest) HttpWebRequest.Create(Host + "process/flip-h/125,0,125,170");
            SetUpRequestCorrectly(req);
            AttachCorrectPicToRequest(req);
            var resp = req.GetResponse();
            SaveFileFromResponse(resp, "TestFileSepia.png");
            Assert.AreEqual(200, (int) ((HttpWebResponse) resp).StatusCode);
        }

        [TestCase("flip-v")]
        [TestCase("flip-h")]
        [TestCase("rotate-cw")]
        [TestCase("rotate-cww")]
        public void ResponseWithNoContent_WhenThereIsNoIntersections(string transform)
        {
            var req = (HttpWebRequest) HttpWebRequest.Create(Host + $"process/{transform}/-1,0,-10,15");
            SetUpRequestCorrectly(req);
            AttachCorrectPicToRequest(req);
            var resp = req.GetResponse();
            Assert.AreEqual(204, (int) ((HttpWebResponse) resp).StatusCode);
        }

        [Test]
        public void RefuseOver100KBPic()
        {
            var req = (HttpWebRequest) HttpWebRequest.Create(Host + "process/flip-h/0,0,300,100");
            SetUpRequestCorrectly(req);
            AttachTooLargePicToRequest(req);
            AssertResponseHasFailCode(req, 400);
        }


        [Test]
        public void RefuseNotPostRequest()
        {
            var req = (HttpWebRequest) HttpWebRequest.Create(Host + "process/flip-h/0,0,300,100");
            req.Method = "PUT";
            req.ContentType = "application/octet-stream";
            req.KeepAlive = true;
            AttachCorrectPicToRequest(req);
            AssertResponseHasFailCode(req, 400);
        }

        [TestCase("bad/process/flip-h/300,30,300,100")]
        [TestCase("process/flip-h/300,30,300")]
        [TestCase("process/flip-h/300,30,300,sym")]
        [TestCase("process/flip-h/300;30;300;100")]
        [TestCase("bad/flip-h/300,30,300,100")]
        [TestCase("flip-h/300,30,300,100")]
        [TestCase("flip-h/300,30,300,100/process")]
        [TestCase("300,30,300,100/flip-h/process")]
        [TestCase("process/h/0,0,512,512")]
        public void RefuseWrongPathFormat(string path)
        {
            var req = (HttpWebRequest) HttpWebRequest.Create(Host + path);
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