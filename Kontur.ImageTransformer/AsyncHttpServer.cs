using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using NLog.Targets;

namespace Kontur.ImageTransformer
{
    public class AsyncHttpServer : IDisposable
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private readonly HttpListener listener;
        private Thread listenerThread;
        private Thread queueHandleThread;
        private bool disposed;
        private volatile bool isRunning;
        private readonly TimeSpan requestWaitingLimit = TimeSpan.FromMilliseconds(300);
        private BlockingCollection<ImageController> queue;

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
                    queueHandleThread = new Thread(HandleQueueElems)
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.AboveNormal
                    };
                    queueHandleThread.Start();

                    listenerThread = new Thread(Listen)
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.AboveNormal
                    };
                    listenerThread.Start();
                    isRunning = true;
                    logger.Info("Server started");
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

                logger.Info("Server stopped");
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
                        var listenerContext = listener.GetContext();
                        var controller = new ImageController(listenerContext);
                        Task.Run(() =>
                        {
                            if (!queue.TryAdd(controller))
                                logger.Error($"Context doesn't be added to queue, controller: " + DefaultJsonSerializer.Instance.SerializeObject(controller));
                            
                        });


                    }
                    else Thread.Sleep(0);
                }
                catch (ThreadAbortException e)
                {
                    logger.Error(e, "Thread aborted");
                    return;
                }
                catch (Exception error)
                {
                    logger.Error(error);
                }
            }
        }

        
        private void HandleQueueElems()
        {
            foreach (var controller in queue.GetConsumingEnumerable())
            {
                if (DateTime.Now - controller.Start < requestWaitingLimit)
                    Task.Run(() =>
                    {
                        controller.HandleRequest();
                    });
                else
                    Task.Run(() =>
                    {
                        controller.RefuseRequestSafely();
                    });
            }
        }
        
    }
}