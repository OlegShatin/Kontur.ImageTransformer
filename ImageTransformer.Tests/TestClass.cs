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
    public class TestClass
    {
        private AsyncHttpServer server;

        [OneTimeSetUp]
        public void SetUp()
        {
            server = new AsyncHttpServer();
            server.Start("http://localhost:8080/");
        }

        [Test]
        public void TestMethod()
        {
            var req = (HttpWebRequest)HttpWebRequest.Create("http://localhost:8080/" + "process/filter/coords");
            req.Method = "GET";
            req.GetResponse();
            Assert.Pass("Your first passing test");
        }

        [Test]
        public void UploadCorrectPic()
        {
            var req = (HttpWebRequest)HttpWebRequest.Create("http://localhost:8080/" + "process/grayscale/300,30,300,100");
            req.Method = "POST";
            req.ContentType = "application/octet-stream";
            req.KeepAlive = true;
            

            Stream newStream = req.GetRequestStream();

            File.OpenRead("B:\\home\\oleg\\code\\CSharpProjects\\KonturImageTransformer\\ImageTransformer.Tests\\Lenna.png").CopyTo(newStream);
            var resp = req.GetResponse();
            using (var stream = resp.GetResponseStream())
            {
                stream.CopyTo(File.Create("B:\\home\\oleg\\code\\CSharpProjects\\KonturImageTransformer\\ImageTransformer.Tests\\TestFile.png"));
            }

            Assert.AreEqual(200, (int)((HttpWebResponse)resp).StatusCode);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            server.Stop();
        }

    }
}
