using System;
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
                currentReqsNumber++;
                try
                {
                    if (listener.IsListening)
                    {
                        var listenerContext = listener.GetContext();
                        if (currentReqsNumber < maxReqs)
                        {
                            
                            var controller = new ImageController(listenerContext, currentReqsNumber);
                            Task.Run(() =>
                            {
                                controller.HandleRequest();
                                var ts = DateTime.Now - controller.Start;
                                if (!IsMax && ts > TimeSpan.FromMilliseconds(950))
                                {
                                    //Console.WriteLine("max reqs upd: " + currentReqsNumber);
                                    //Console.WriteLine("Timespan: " + ts);

                                    IsMax = true;
                                    maxReqs = controller.ReqsNumber;
                                }

                                currentReqsNumber--;
                                //Console.WriteLine("Successed: " + (DateTime.Now - controller.Start));
                            });
                        }
                        else
                        {
                            //Task.Run(() => {
                            listenerContext.Response.StatusCode = 503;
                            listenerContext.Response.Close();
                            //Console.WriteLine("Refused, maxReqs, currentReqs: " + maxReqs + ", " + currentReqsNumber);
                            currentReqsNumber--;
                            //});
                        }
                    }
                    else Thread.Sleep(0);
                }
                catch (ThreadAbortException e)
                {
                    Console.WriteLine(e);
                    currentReqsNumber--;
                    return;
                }
                catch (Exception error)
                {
                    Console.WriteLine(error);
                    currentReqsNumber--;
                }
            }
        }

        private bool IsMax = false;
        private int maxReqs = int.MaxValue;
        private int currentReqsNumber = 0;

        private void HandleContext(HttpListenerContext listenerContext)
        {
            // listenerContext.Response.StatusCode = (int) HttpStatusCode.OK;
//           using (var writer = new StreamWriter(listenerContext.Response.OutputStream))
//                writer.WriteLine("Hello, world!");
        }

        private readonly HttpListener listener;

        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;
    }
}