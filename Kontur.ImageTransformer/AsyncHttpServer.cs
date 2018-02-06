using System;
using System.Collections.Concurrent;
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

                    queue = new BlockingCollection<ImageController>(new ConcurrentQueue<ImageController>());
                    Task.Run((Action) HandleQueueElems);

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

                queueHandleThread.Abort();
                queueHandleThread.Join();

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

        private int i = 0;
        private void Listen()
        {
            
            //ThreadPool.SetMinThreads(60, 1000);
            while (true)
            {
                
                try
                {
                    if (listener.IsListening)
                    {
                        var listenerContext = listener.GetContext();
                        var controller = new ImageController(listenerContext);
                        Task.Run(() =>
                        {
                            while (!queue.TryAdd(controller))
                            {
                                Console.WriteLine("not added");
                            }
                           // Console.WriteLine(i++);
                        });


                    }
                    else Thread.Sleep(0);
                }
                catch (ThreadAbortException e)
                {
                    Console.WriteLine(e);
                    return;
                }
                catch (Exception error)
                {
                    Console.WriteLine(error);
                }
            }
        }

        private TimeSpan halfSecond = TimeSpan.FromMilliseconds(300);
        private void HandleQueueElems()
        {
            foreach (var controller in queue.GetConsumingEnumerable())
            {
                if (DateTime.Now - controller.Start < halfSecond)
                    Task.Run(() =>
                    {
                        controller.HandleRequest();
                    });
                else
                    Task.Run(() =>
                    {
                        using (controller.Response)
                        {
                            controller.RefuseRequest();
                        }

                    });
            }
        }

        private BlockingCollection<ImageController> queue;
        private void HandleRequest(Controller controller)
        {
            
            Task.Run(() =>
            {
                controller.HandleRequest();
            });
        }

        private void RefuseRequest(Controller controller)
        {
            Task.Run(() =>
            {
                using (controller.Response)
                {
                    controller.RefuseRequest();
                }
                
            });
        }

        private void HandleContext(HttpListenerContext listenerContext)
        {
            // listenerContext.Response.StatusCode = (int) HttpStatusCode.OK;
//           using (var writer = new StreamWriter(listenerContext.Response.OutputStream))
//                writer.WriteLine("Hello, world!");
        }

        private readonly HttpListener listener;

        private Thread listenerThread;
        private Thread queueHandleThread;
        private bool disposed;
        private volatile bool isRunning;
    }
}