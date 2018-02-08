using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;


namespace Kontur.ImageTransformer
{
    public class ImageController : Controller
    {
        private ImageHandler imageHandler = new ImageHandler();
        public DateTime Start { get; private set; }
        private const int BytesLimit = 100 * 1024;

        public ImageController(HttpListenerContext listenerContext) : base(listenerContext)
        {
            Start = DateTime.Now;
        }

        public override void HandleRequest()
        {
            using (Response)
            {
                if (Request.Url.Segments.Length == 4 && Request.Url.Segments[1] == "process/" &&
                    Request.ContentLength64 < BytesLimit && Request.HttpMethod == "POST")
                {
                    int x, y, height, width;
                    if (!TryParseCoords(Request.Url.Segments[3], out x, out y, out height, out width))
                    {
                        SendBadRequest();
                        return;
                    }

                    var segment = Request.Url.Segments[2];
                    var filter = segment.StartsWith("threshold(") && segment.EndsWith(")/")
                        ? "threshold"
                        : segment.Substring(0, segment.Length - 1);
                    switch (filter)
                    {
                        case "threshold":
                            int level;
                            if (TryParseParam(segment, out level))
                                HandleThreshold(level, x, y, height, width);
                            else
                                SendBadRequest();
                            break;
                        case "sepia":
                            HandleSepia(x, y, height, width);
                            break;
                        case "grayscale":
                            HandleGrayscale(x, y, height, width);
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
        }

        private bool TryParseParam(string segment, out int level)
        {
            try
            {
                int firstDigitIndex = (segment.IndexOf("(") + 1);
                int lastBraceIndex = (segment.IndexOf(")"));
                return int.TryParse(segment.Substring(firstDigitIndex, lastBraceIndex - firstDigitIndex), out level);
            }
            catch (Exception e)
            {
                level = 0;
                return false;
            }
        }

        private bool TryParseCoords(string sourse, out int x, out int y, out int height, out int width)
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

        private void HandleThreshold(int level, int x, int y, int height, int width)
        {
            if (level < 0 || level > 100)
                SendBadRequest();
            else
                HandlePicSegment(x, y, height, width, new ThresholdBitmapAction(level, imageHandler));
        }

        private void HandleGrayscale(int x, int y, int height, int width)
        {
            HandlePicSegment(x, y, height, width, new GrayscaleBitmapAction(imageHandler));
        }

        private void HandleSepia(int x, int y, int height, int width)
        {
            HandlePicSegment(x, y, height, width, new SepiaBitmapAction(imageHandler));
        }

        private delegate void SegmentHandler(Bitmap segment);

        //private delegate void SegmentHandlerWithParam(Bitmap segment, int param);

        private void HandlePicSegment(int x, int y, int height, int width, BitmapAction action)
        {
            using (Request.InputStream)
            {
                Bitmap pic;


                pic = new Bitmap(Request.InputStream);


                Bitmap segment;
                if (!imageHandler.TryCropImage(pic, out segment, x, y, height, width))
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