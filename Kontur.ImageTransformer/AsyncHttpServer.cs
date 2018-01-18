using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer
{
    public class AsyncHttpServer : IDisposable
    {
        public AsyncHttpServer()
        {
            listener = new HttpListener();
        }

        public void Start(string prefix)
        {
            lock (listener)
            {
                if (!isRunning)
                {
                    listener.Prefixes.Clear();
                    listener.Prefixes.Add(prefix);
                    listener.Start();

                    listenerThread = new Thread(Listen)
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.Highest
                    };
                    listenerThread.Start();

                    isRunning = true;
                }
            }
        }

        public void Stop()
        {
            lock (listener)
            {
                if (!isRunning)
                    return;

                listener.Stop();

                listenerThread.Abort();
                listenerThread.Join();

                isRunning = false;
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            Stop();

            listener.Close();
        }

        private void Listen()
        {
            while (true)
            {
                try
                {
                    if (listener.IsListening)
                    {
                        var context = listener.GetContext();
                        Task.Run(() => HandleContextAsync(context));
                    }
                    else Thread.Sleep(0);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception error)
                {
                    // TODO: log errors
                }
            }
        }

        private async Task HandleContextAsync(HttpListenerContext listenerContext)
        {
            var controller = new ImageController(listenerContext);
            await controller.HandleRequestAsync();
            // listenerContext.Response.StatusCode = (int) HttpStatusCode.OK;
//           using (var writer = new StreamWriter(listenerContext.Response.OutputStream))
//                writer.WriteLine("Hello, world!");
        }

        private readonly HttpListener listener;

        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;
    }

    public abstract class Controller
    {
        public HttpListenerRequest Request { get; private set; }
        public HttpListenerResponse Response { get; private set; }

        protected Controller(HttpListenerContext listenerContext)
        {
            Request = listenerContext.Request;
            Response = listenerContext.Response;
        }

        public abstract Task HandleRequestAsync();

        protected Task SendBadRequestAsync()
        {
            return Task.Run(() =>
            {
                Response.StatusCode = (int) HttpStatusCode.BadRequest;
                Response.Close();
            });
        }

        protected Task SendNotFoundAsync()
        {
            return Task.Run(() =>
            {
                Response.StatusCode = (int) HttpStatusCode.NotFound;
                Response.Close();
            });
        }

        protected Task SendNoContentAsync()
        {
            return Task.Run(() =>
            {
                Response.StatusCode = (int) HttpStatusCode.NoContent;
                Response.Close();
            });
        }
    }

    public class ImageController : Controller
    {
        private ImageHandler imageHandler = new ImageHandler();

        public ImageController(HttpListenerContext listenerContext) : base(listenerContext)
        {
        }

        public override async Task HandleRequestAsync()
        {
            if (Request.Url.Segments.Length == 4 && Request.Url.Segments[1] == "process/")
            {
                int x, y, height, width;
                if (!TryParseCoords(Request.Url.Segments[3], out x, out y, out height, out width))
                {
                    await SendBadRequestAsync();
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
                            await HandleThreshold(level, x, y, height, width);
                        else
                            await SendBadRequestAsync();
                        break;
                    case "sepia":
                        await HandleSepia(x, y, height, width);
                        break;
                    case "grayscale":
                        await HandleGrayscale(x, y, height, width);
                        break;
                    default:
                        await SendBadRequestAsync();
                        break;
                }
            }
            else
            {
                await SendBadRequestAsync();
            }
        }

        private bool TryParseParam(string segment, out int level)
        {
            try
            {
                return int.TryParse(segment.Substring((segment.IndexOf("(") + 1), (segment.IndexOf(")") + 1)),
                    out level);
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

        private async Task HandleThreshold(int level, int x, int y, int height, int width)
        {
        }

        private async Task HandleGrayscale(int x, int y, int height, int width)
        {
            await HandlePicSegment(x, y, height, width, imageHandler.ApplyGrayscale);
        }

        private async Task HandleSepia(int x, int y, int height, int width)
        {
            await HandlePicSegment(x, y, height, width, imageHandler.ApplySepia);
        }

        private delegate void SegmentHandler(Bitmap segment);
        private async Task HandlePicSegment(int x, int y, int height, int width, SegmentHandler handler)
        {
            using (Request.InputStream)
            {
                Bitmap pic = new Bitmap(Request.InputStream);
                Bitmap segment;
                if (!imageHandler.TryCropImage(pic, out segment, x, y, height, width))
                    await SendNoContentAsync();
                else
                {
                    handler.Invoke(segment);
                    using (Response.OutputStream)
                        segment.Save(Response.OutputStream, ImageFormat.Png);
                }
                
            }
            Response.Close();
        }
    }

    public class ImageHandler
    {
        public bool TryCropImage(Bitmap source, out Bitmap result, int x, int y, int height, int width)
        {
            result = null;
            //normalize X and Y
            if (width < 0)
            {
                x += width;
                width = -width;
            }

            if (height < 0)
            {
                y += height;
                height = -height;
            }

            if (x > source.Width || y > source.Height || (x + width) < 0 || (y + height) < 0)
                return false;

            int trueX, trueY, trueWidth, trueHeigth;
            if (x < 0)
            {
                trueX = 0;
                trueWidth = width + x < source.Width ? width + x : source.Width;
            }
            else
            {
                trueX = x;
                trueWidth = x + width > source.Width - x ? source.Width - x : width;
            }

            if (y < 0)
            {
                trueY = 0;
                trueHeigth = height + y < source.Height ? height + y : source.Height;
            }
            else
            {
                trueY = y;
                trueHeigth = y + height > source.Height - y ? source.Height - y : height;
            }

            // An empty bitmap which will hold the cropped image
            result = new Bitmap(trueWidth, trueHeigth);

            Graphics g = Graphics.FromImage(result);

            // Draw the given area (section) of the source image
            // at location 0,0 on the empty bitmap (bmp)
            g.DrawImage(source, 0, 0, new Rectangle(trueX, trueY, trueWidth, trueHeigth), GraphicsUnit.Pixel);

            return true;
        }

        public void ApplySepia(Bitmap processedBitmap)
        {
            ProcessUsingLockbitsAndUnsafeAndParallel(processedBitmap, SepiaPixelAction);
        }

        private void SepiaPixelAction(ref int r, ref int g, ref int b)
        {
            int newR = (int) (r * 0.393f + g * 0.769f + b * 0.189f);
            int newG = (int) (r * 0.349f + g * 0.686f + b * 0.168f);
            int newB = (int) (r * 0.272f + g * 0.543f + b * 0.131f);
            r = newR > 255 ? 255 : newR;
            g = newG > 255 ? 255 : newG;
            b = newB > 255 ? 255 : newB;
        }

        private delegate void PixelActionDel(ref int r, ref int g, ref int b);

        private unsafe void ProcessUsingLockbitsAndUnsafeAndParallel(Bitmap processedBitmap,
            PixelActionDel actionForRgbPixels)
        {
            BitmapData bitmapData = processedBitmap.LockBits(
                new Rectangle(0, 0, processedBitmap.Width, processedBitmap.Height), ImageLockMode.ReadWrite,
                processedBitmap.PixelFormat);

            int bytesPerPixel = Bitmap.GetPixelFormatSize(processedBitmap.PixelFormat) / 8;
            int heightInPixels = bitmapData.Height;
            int widthInBytes = bitmapData.Width * bytesPerPixel;
            byte* ptrFirstPixel = (byte*) bitmapData.Scan0;

            Parallel.For(0, heightInPixels, y =>
            {
                byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);
                for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                {
                    int blue = currentLine[x];
                    int green = currentLine[x + 1];
                    int red = currentLine[x + 2];
                    actionForRgbPixels.Invoke(ref red, ref green, ref blue);
                    currentLine[x] = (byte) blue;
                    currentLine[x + 1] = (byte) green;
                    currentLine[x + 2] = (byte) red;
                }
            });
            processedBitmap.UnlockBits(bitmapData);
        }

        public void ApplyGrayscale(Bitmap processedBitmap)
        {
            ProcessUsingLockbitsAndUnsafeAndParallel(processedBitmap, GrayscalePixelAction);
        }
        private void GrayscalePixelAction(ref int r, ref int g, ref int b)
        {
            int intensity = (r + g + b) / 3;
            r = intensity;
            g = intensity;
            b = intensity;
        }
    }
}