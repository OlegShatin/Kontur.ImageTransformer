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

        private async Task HandleThreshold(int level, int x, int y, int height, int width)
        {
            if (level < 0 || level > 100)
                await SendBadRequestAsync();
            else
                await HandlePicSegmentWithParam(x, y, height, width, imageHandler.ApplyThreshold, level);
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
        private delegate void SegmentHandlerWithParam(Bitmap segment, int param);

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
                    Response.Close();
                }
                
            }
            
        }
        private async Task HandlePicSegmentWithParam(int x, int y, int height, int width, SegmentHandlerWithParam handlerWithParam, int handlerParam)
        {
            using (Request.InputStream)
            {
                Bitmap pic = new Bitmap(Request.InputStream);
                Bitmap segment;
                if (!imageHandler.TryCropImage(pic, out segment, x, y, height, width))
                    await SendNoContentAsync();
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