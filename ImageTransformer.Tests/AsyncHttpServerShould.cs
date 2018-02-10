using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Kontur.ImageTransformer;

namespace ImageTransformer.Tests
{
    [TestFixture]
    public class AsyncHttpServerShould
    {
        private AsyncHttpServer server;

        private const string ValidPicPath =
            "B:\\home\\oleg\\code\\csharp\\CSharpProjects\\KonturImageTransformer\\ImageTransformer.Tests\\LennaShort.png";

        private const string LargePicPath =
            "B:\\home\\oleg\\code\\csharp\\CSharpProjects\\KonturImageTransformer\\ImageTransformer.Tests\\Lenna.png";

        private const string StoragePath =
            "B:\\home\\oleg\\code\\csharp\\CSharpProjects\\KonturImageTransformer\\ImageTransformer.Tests\\";
        [OneTimeSetUp]
        public void SetUp()
        {
            server = new AsyncHttpServer();
            server.Start("http://localhost:8080/");
        }

        

        [Test]
        public void UploadCorrectPicGrayscale()
        {
            var req = (HttpWebRequest)HttpWebRequest.Create("http://localhost:8080/" + "process/grayscale/0,0,10,15");
            req.Method = "POST";
            req.ContentType = "application/octet-stream";
            req.KeepAlive = true;
            

            Stream newStream = req.GetRequestStream();

            File.OpenRead(ValidPicPath).CopyTo(newStream);
            var resp = req.GetResponse();
            using (var stream = resp.GetResponseStream())
            {
                stream.CopyTo(File.Create(StoragePath + "TestFileGrayscale.png"));
            }

            Assert.AreEqual(200, (int)((HttpWebResponse)resp).StatusCode);
        }
        [Test]
        public void UploadCorrectPicThreshold()
        {
            var req = (HttpWebRequest)HttpWebRequest.Create("http://localhost:8080/" + "process/threshold(50)/0,0,512,512");
            req.Method = "POST";
            req.ContentType = "application/octet-stream";
            req.KeepAlive = true;


            Stream newStream = req.GetRequestStream();

            File.OpenRead(ValidPicPath).CopyTo(newStream);
            var resp = req.GetResponse();
            using (var stream = resp.GetResponseStream())
            {
                stream.CopyTo(File.Create(StoragePath + "TestFileThreshold.png"));
            }

            Assert.AreEqual(200, (int)((HttpWebResponse)resp).StatusCode);
        }
        [Test]
        public void UploadCorrectPicSepia()
        {
            var req = (HttpWebRequest)HttpWebRequest.Create("http://localhost:8080/" + "process/sepia/0,0,10,15");
            req.Method = "POST";
            req.ContentType = "application/octet-stream";
            req.KeepAlive = true;


            Stream newStream = req.GetRequestStream();

            File.OpenRead(ValidPicPath).CopyTo(newStream);
            var resp = req.GetResponse();
            using (var stream = resp.GetResponseStream())
            {
                stream.CopyTo(File.Create(StoragePath + "TestFileSepia.png"));
            }

            Assert.AreEqual(200, (int)((HttpWebResponse)resp).StatusCode);
        }

        [Test]
        public void RefuseOver100KBPic()
        {
            var req = (HttpWebRequest)HttpWebRequest.Create("http://localhost:8080/" + "process/sepia/300,30,300,100");
            req.Method = "POST";
            req.ContentType = "application/octet-stream";
            req.KeepAlive = true;


            Stream newStream = req.GetRequestStream();

            File.OpenRead(LargePicPath).CopyTo(newStream);
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
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    Assert.AreEqual(400, (int)httpResponse.StatusCode);
                }
            }
            
        }

        [Test]
        public void RefuseNotPostRequest()
        {
            var req = (HttpWebRequest)HttpWebRequest.Create("http://localhost:8080/" + "process/sepia/300,30,300,100");
            req.Method = "PUT";
            req.ContentType = "application/octet-stream";
            req.KeepAlive = true;


            
            Stream newStream = req.GetRequestStream();

            File.OpenRead(ValidPicPath).CopyTo(newStream);
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
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    Assert.AreEqual(400, (int)httpResponse.StatusCode);
                }
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            server.Stop();
        }

    }
}
