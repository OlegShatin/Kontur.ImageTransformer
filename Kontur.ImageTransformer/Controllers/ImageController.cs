using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using Kontur.ImageTransformer.Services;
using NLog;


namespace Kontur.ImageTransformer.Controllers
{
    public class ImageController : Controller
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private readonly ImageHandler imageHandler = new ImageHandler();
        private const int BytesLimit = 100 * 1024;

        public ImageController(HttpListenerContext listenerContext) : base(listenerContext)
        {
        }

        public override void HandleRequest()
        {
            using (Response)
            {
                try
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

                        var transformSegment = Request.Url.Segments[2];
                        var transform = transformSegment.Substring(0, transformSegment.Length - 1);
                        switch (transform)
                        {
                            case "rotate-cw":
                                HandleRotateCw(x, y, width, height);
                                break;
                            case "rotate-cww":
                                HandleRotateCww(x, y, width, height);
                                break;
                            case "flip-v":
                                HandleFlipVertical(x, y, width, height);
                                break;
                            case "flip-h":
                                HandleFlipHorizontal(x, y, width, height);
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
                catch (Exception e)
                {
                    logger.Error(e);
                }
            }
        }

        //next methods pick right CropAndHandleStrategy and run common crop-and-change-pic flow with this strategy

        private void HandleFlipHorizontal(int x, int y, int width, int height)
        {
            HandlePicSegment(x, y, width, height, new FlipHorizontalStrategy(imageHandler));
        }

        private void HandleFlipVertical(int x, int y, int width, int height)
        {
            HandlePicSegment(x, y, width, height, new FlipVerticalStrategy(imageHandler));
        }

        private void HandleRotateCww(int x, int y, int width, int height)
        {
            HandlePicSegment(x, y, width, height, new RotateCwwStrategy(imageHandler));
        }

        private void HandleRotateCw(int x, int y, int width, int height)
        {
            HandlePicSegment(x, y, width, height, new RotateCwStrategy(imageHandler));
        }

        #region Common methods

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

        /// <summary>
        /// common impl for crop and change pic flow
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="cropAndHandleStrategy">Define how to crop and change bitmap</param>
        private void HandlePicSegment(int x, int y, int width, int height,
            BitmapCropAndHandleStrategy cropAndHandleStrategy)
        {
            using (Request.InputStream)
            {
                Bitmap pic = new Bitmap(Request.InputStream);
                Bitmap segment;
                if (!cropAndHandleStrategy.TryCrop(pic, out segment, x, y, width, height))
                    SendNoContent();
                else
                {
                    cropAndHandleStrategy.ApplyChangesToImage(segment);
                    using (Response.OutputStream)
                        segment.Save(Response.OutputStream, ImageFormat.Png);
                    Response.Close();
                }
            }
        }

        #endregion

        #region CropAndHandleStrategies

        /// <summary>
        /// Class incapsulates methods used for crop and change bitmaps by HandlePicSegment method
        /// </summary>
        private abstract class BitmapCropAndHandleStrategy
        {
            protected readonly ImageHandler performer;

            protected BitmapCropAndHandleStrategy(ImageHandler performer)
            {
                this.performer = performer;
            }

            public abstract void ApplyChangesToImage(Bitmap bitmap);
            public abstract bool TryCrop(Bitmap source, out Bitmap result, int x, int y, int width, int height);
        }

        private class FlipHorizontalStrategy : BitmapCropAndHandleStrategy
        {
            public FlipHorizontalStrategy(ImageHandler performer) : base(performer)
            {
            }

            public override void ApplyChangesToImage(Bitmap bitmap) => performer.FlipHorizontal(bitmap);
            /// <summary>
            /// Method  tries crop segment of source with using reverse-transformed coordinates to minimize 
            /// size of bitmap which will need to be flipped on ApplyChangesToImage
            /// </summary>
            /// <param name="source"></param>
            /// <param name="result"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="width"></param>
            /// <param name="height"></param>
            /// <returns>Cropping is successed</returns>
            public override bool TryCrop(Bitmap source, out Bitmap result, int x, int y, int width, int height)
            {
                return performer.TryCropImage(source, out result,
                    TransformCoords.ToFlipH.GetX(x, source.Width),
                    TransformCoords.ToFlipH.GetY(y),
                    TransformCoords.ToFlipH.GetWidth(height),
                    TransformCoords.ToFlipH.GetHeight(width));
            }
        }

        private class FlipVerticalStrategy : BitmapCropAndHandleStrategy
        {
            public FlipVerticalStrategy(ImageHandler performer) : base(performer)
            {
            }

            public override void ApplyChangesToImage(Bitmap bitmap) => performer.FlipVertical(bitmap);
            /// <summary>
            /// Method  tries crop segment of source with using reverse-transformed coordinates to minimize 
            /// size of bitmap which will need to be flipped on ApplyChangesToImage
            /// </summary>
            /// <param name="source"></param>
            /// <param name="result"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="width"></param>
            /// <param name="height"></param>
            /// <returns>Cropping is successed</returns>
            public override bool TryCrop(Bitmap source, out Bitmap result, int x, int y, int width, int height)
            {
                return performer.TryCropImage(source, out result,
                    TransformCoords.ToFlipV.GetX(x),
                    TransformCoords.ToFlipV.GetY(y, source.Height),
                    TransformCoords.ToFlipV.GetWidth(height),
                    TransformCoords.ToFlipV.GetHeight(width));
            }
        }

        private class RotateCwwStrategy : BitmapCropAndHandleStrategy
        {
            public RotateCwwStrategy(ImageHandler performer) : base(performer)
            {
            }

            public override void ApplyChangesToImage(Bitmap bitmap) => performer.RotateCww(bitmap);
            /// <summary>
            /// Method  tries crop segment of source with using reverse-transformed (cw-rotated) coordinates to minimize 
            /// size of bitmap which will need to be rotated on ApplyChangesToImage
            /// </summary>
            /// <param name="source"></param>
            /// <param name="result"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="width"></param>
            /// <param name="height"></param>
            /// <returns>Cropping is successed</returns>
            public override bool TryCrop(Bitmap source, out Bitmap result, int x, int y, int width, int height)
            {
                return performer.TryCropImage(source, out result,
                    TransformCoords.ToCw.GetX(y, source.Height),
                    TransformCoords.ToCw.GetY(x),
                    TransformCoords.ToCw.GetWidth(height),
                    TransformCoords.ToCw.GetHeight(width));
            }
        }

        private class RotateCwStrategy : BitmapCropAndHandleStrategy
        {
            public RotateCwStrategy(ImageHandler performer) : base(performer)
            {
            }

            public override void ApplyChangesToImage(Bitmap bitmap) => performer.RotateCw(bitmap);
            /// <summary>
            /// Method  tries crop segment of source with using reverse-transformed (cww-rotated) coordinates to minimize 
            /// size of bitmap which will need to be rotated on ApplyChangesToImage
            /// </summary>
            /// <param name="source"></param>
            /// <param name="result"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="width"></param>
            /// <param name="height"></param>
            /// <returns>Cropping is successed</returns>
            public override bool TryCrop(Bitmap source, out Bitmap result, int x, int y, int width, int height)
            {
                return performer.TryCropImage(source, out result,
                    TransformCoords.ToCww.GetX(y),
                    TransformCoords.ToCww.GetY(x, source.Width),
                    TransformCoords.ToCww.GetWidth(height),
                    TransformCoords.ToCww.GetHeight(width));
            }
        }

        #endregion

        #region Obsolete - Filters

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

        private void HandleThreshold(int level, int x, int y, int width, int height)
        {
            if (level < 0 || level > 100)
                SendBadRequest();
            else
                HandlePicSegment(x, y, width, height, new ThresholdBitmapCropAndHandleStrategy(level, imageHandler));
        }

        private void HandleGrayscale(int x, int y, int width, int height)
        {
            HandlePicSegment(x, y, width, height, new GrayscaleBitmapCropAndHandleStrategy(imageHandler));
        }

        private void HandleSepia(int x, int y, int width, int height)
        {
            HandlePicSegment(x, y, width, height, new SepiaBitmapCropAndHandleStrategy(imageHandler));
        }

        #region Obsolete - Filters Strategies

        private abstract class BitmapCropAndFilterStrategy : BitmapCropAndHandleStrategy
        {
            protected BitmapCropAndFilterStrategy(ImageHandler performer) : base(performer)
            {
            }

            public override bool TryCrop(Bitmap source, out Bitmap result, int x, int y, int width, int height)
            {
                return performer.TryCropImage(source, out result, x, y, width, height);
            }
        }


        private class ThresholdBitmapCropAndHandleStrategy : BitmapCropAndFilterStrategy
        {
            private readonly int level;

            public ThresholdBitmapCropAndHandleStrategy(int level, ImageHandler performer) : base(performer)
            {
                this.level = level;
            }

            public override void ApplyChangesToImage(Bitmap bitmap)
            {
                performer.ApplyThreshold(bitmap, level);
            }
        }


        private class SepiaBitmapCropAndHandleStrategy : BitmapCropAndFilterStrategy
        {
            public SepiaBitmapCropAndHandleStrategy(ImageHandler performer) : base(performer)
            {
            }

            public override void ApplyChangesToImage(Bitmap bitmap)
            {
                performer.ApplySepia(bitmap);
            }
        }


        private class GrayscaleBitmapCropAndHandleStrategy : BitmapCropAndFilterStrategy
        {
            public GrayscaleBitmapCropAndHandleStrategy(ImageHandler performer) : base(performer)
            {
            }

            public override void ApplyChangesToImage(Bitmap bitmap)
            {
                performer.ApplyGrayscale(bitmap);
            }
        }

        #endregion

        #endregion
    }
}