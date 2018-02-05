using System;
using System.IO;
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
            int threadsCount;
            ThreadPool.SetMinThreads(60, 1000);
            while (true)
            {
                currentReqsNumber++;
                try
                {
                    if (listener.IsListening)
                    {
                        var listenerContext = listener.GetContext();
                        //ThreadPool.GetMaxThreads(out threadsCount, out int i);
                        //ThreadPool.GetAvailableThreads(out int avalThreads,out  i);
                        //ThreadPool.GetMinThreads(out int minThreads, out i);
                        //Console.WriteLine(threadsCount + " " + avalThreads + " " + minThreads);
                        if (currentReqsNumber < maxReqs)
                        {
                            var controller = new ImageController(listenerContext, currentReqsNumber);
                            Task.Run(() =>
                            {
                                controller.HandleRequest();
                                var ts = DateTime.Now - controller.Start;
                                if (ts > TimeSpan.FromMilliseconds(500))
                                    Console.WriteLine("Timespan: " + ts);
                                if (!IsMax && ts > TimeSpan.FromMilliseconds(500))
                                {
                                    //Console.WriteLine("max reqs upd: " + currentReqsNumber);
                                   

                                    IsMax = true;
                                    maxReqs = controller.ReqsNumber;
                                    Console.WriteLine("Timespan: " + ts + " , reqs: " + maxReqs);
                                }

                                currentReqsNumber--;
                                //Console.WriteLine("Successed: " + (DateTime.Now - controller.Start));
                            });
                        }
                        else
                        {
                            Task.Run(() =>
                            {
                                listenerContext.Response.StatusCode = 429;
                                listenerContext.Response.Close();

                                //Console.WriteLine("Refused, maxReqs, currentReqs: " + maxReqs + ", " + currentReqsNumber);
                                currentReqsNumber--;
                            });
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