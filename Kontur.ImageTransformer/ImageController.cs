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

        public ImageController(HttpListenerContext listenerContext) : base(listenerContext)
        {
            Start = DateTime.Now;
        }

        public override void HandleRequest()
        {
            using (Response)
            {
                if (Request.Url.Segments.Length == 4 && Request.Url.Segments[1] == "process/")
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
                HandlePicSegmentWithParam(x, y, height, width, imageHandler.ApplyThreshold, level);
        }

        private void HandleGrayscale(int x, int y, int height, int width)
        {
            HandlePicSegment(x, y, height, width, imageHandler.ApplyGrayscale);
        }

        private void HandleSepia(int x, int y, int height, int width)
        {
            HandlePicSegment(x, y, height, width, imageHandler.ApplySepia);
        }

        private delegate void SegmentHandler(Bitmap segment);

        private delegate void SegmentHandlerWithParam(Bitmap segment, int param);

        private void HandlePicSegment(int x, int y, int height, int width, SegmentHandler handler)
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
                    handler.Invoke(segment);
                    using (Response.OutputStream)
                        segment.Save(Response.OutputStream, ImageFormat.Png);
                    Response.Close();
                }
            }
        }

        private void HandlePicSegmentWithParam(int x, int y, int height, int width,
            SegmentHandlerWithParam handlerWithParam, int handlerParam)
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
                    handlerWithParam.Invoke(segment, handlerParam);
                    using (Response.OutputStream)
                        segment.Save(Response.OutputStream, ImageFormat.Png);
                    Response.Close();
                }
            }
        }
    }
}