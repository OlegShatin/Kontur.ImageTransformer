using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using Ether.Network.Packets;
using Kontur.ImageTransformer.Services;


namespace Kontur.ImageTransformer.Controllers
{
    public class ImageController : Controller
    {
        private readonly ImageHandler imageHandler = new ImageHandler();
        public DateTime Start { get; private set; }
        private const int BytesLimit = 100 * 1024;

        public ImageController(HttpListenerContext listenerContext) : base(listenerContext)
        {
            Start = DateTime.Now;
        }

        public ImageController(NetPacket netPacket) : base(netPacket)
        {
            
        }

        public override void HandleRequest()
        {
            
                if (Request.Url.Segments.Length == 4 && Request.Url.Segments[1] == "process/" &&
                    Request.ContentLength64 < BytesLimit && Request.HttpMethod == "POST")
                {
                    int x, y, height, width;
                    if (!TryParseCoords(Request.Url.Segments[3], out x, out y, out width, out height))
                    {
                        SendBadRequest();
                        return;
                    }

                    var filterSegment = Request.Url.Segments[2];
                    var filter = filterSegment.StartsWith("threshold(") && filterSegment.EndsWith(")/")
                        ? "threshold"
                        : filterSegment.Substring(0, filterSegment.Length - 1);
                    switch (filter)
                    {
                        case "threshold":
                            int level;
                            if (TryParseParam(filterSegment, out level))
                                HandleThreshold(level, x, y, width, height);
                            else
                                SendBadRequest();
                            break;
                        case "sepia":
                            HandleSepia(x, y, width, height);
                            break;
                        case "grayscale":
                            HandleGrayscale(x, y, width, height);
                            break;
                        default:
                            SendBadRequest();
                            break;
                    }
                }
                else
                {
                    SendBadRequest();
                }
            
        }

        private bool TryParseParam(string segment, out int level)
        {
            try
            {
                int firstDigitIndex = (segment.IndexOf("(") + 1);
                int lastBraceIndex = (segment.IndexOf(")"));
                return int.TryParse(segment.Substring(firstDigitIndex, lastBraceIndex - firstDigitIndex), out level);
            }
            catch (Exception)
            {
                level = 0;
                return false;
            }
        }

        private bool TryParseCoords(string sourse, out int x, out int y, out int width, out int height)
        {
            x = 0;
            y = 0;
            height = 0;
            width = 0;

            var coords = sourse.Split(',');
            if (coords.Length != 4)
                return false;
            return int.TryParse(coords[0], out x) && int.TryParse(coords[1], out y) &&
                   int.TryParse(coords[2], out width) && int.TryParse(coords[3], out height);
        }

        private void HandleThreshold(int level, int x, int y, int width, int height)
        {
            if (level < 0 || level > 100)
                SendBadRequest();
            else
                HandlePicSegment(x, y, width, height, new ThresholdBitmapAction(level, imageHandler));
        }

        private void HandleGrayscale(int x, int y, int width, int height)
        {
            HandlePicSegment(x, y, width, height, new GrayscaleBitmapAction(imageHandler));
        }

        private void HandleSepia(int x, int y, int width, int height)
        {
            HandlePicSegment(x, y, width, height, new SepiaBitmapAction(imageHandler));
        }


        private void HandlePicSegment(int x, int y, int width, int height, BitmapAction action)
        {
            using (Request.InputStream)
            {
                Bitmap pic = new Bitmap(Request.InputStream);
                Bitmap segment;
                if (!imageHandler.TryCropImage(pic, out segment, x, y, width, height))
                    SendNoContent();
                else
                {
                    action.Invoke(segment);
                    using (Response.OutputStream)
                        segment.Save(Response.OutputStream, ImageFormat.Png);
                    Response.Close();
                }
            }
        }


        #region BitmapActions

        private abstract class BitmapAction
        {
            protected readonly ImageHandler performer;

            protected BitmapAction(ImageHandler performer)
            {
                this.performer = performer;
            }

            public abstract void Invoke(Bitmap bitmap);
        }

        private class ThresholdBitmapAction : BitmapAction
        {
            private readonly int level;

            public ThresholdBitmapAction(int level, ImageHandler performer) : base(performer)
            {
                this.level = level;
            }

            public override void Invoke(Bitmap bitmap)
            {
                performer.ApplyThreshold(bitmap, level);
            }
        }

        private class SepiaBitmapAction : BitmapAction
        {
            public SepiaBitmapAction(ImageHandler performer) : base(performer)
            {
            }

            public override void Invoke(Bitmap bitmap)
            {
                performer.ApplySepia(bitmap);
            }
        }

        private class GrayscaleBitmapAction : BitmapAction
        {
            public GrayscaleBitmapAction(ImageHandler performer) : base(performer)
            {
            }

            public override void Invoke(Bitmap bitmap)
            {
                performer.ApplyGrayscale(bitmap);
            }
        }

        #endregion
    }
}