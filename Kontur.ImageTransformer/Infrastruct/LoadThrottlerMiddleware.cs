using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kontur.ImageTransformer.Controllers;
using PipelineNet.Middleware;

namespace Kontur.ImageTransformer.Infrastruct
{
    public class LoadThrottlerMiddleware : IMiddleware<HttpListenerContext>, IDisposable
    {

        private readonly TimeSpan waitingLimit = TimeSpan.FromMilliseconds(500);

        private BlockingCollection<QueueWaitingEntry<HttpListenerContext>> queue;

        private Thread queueHandleThread;
        Semaphore semaphore = new Semaphore(maxParallelTasks + 1, maxParallelTasks + 1);
        private static readonly int maxParallelTasks = Environment.ProcessorCount * 4;
        private bool disposed;

        public LoadThrottlerMiddleware()
        {
            queue = new BlockingCollection<QueueWaitingEntry<HttpListenerContext>>(new ConcurrentQueue<QueueWaitingEntry<HttpListenerContext>>());
            queueHandleThread = new Thread(HandleQueueElems)
            {
                IsBackground = true
            };
            queueHandleThread.Start();
        }
        public void Run(HttpListenerContext context, Action<HttpListenerContext> next)
        {
            queue.Add(new QueueWaitingEntry<HttpListenerContext>(context, next));
        }
        private void HandleQueueElems()
        {

            foreach (var contextEntry in queue.GetConsumingEnumerable())
            {

                semaphore.WaitOne();
                if (DateTime.Now - contextEntry.EnqueuedAt < waitingLimit)
                    Task.Factory.StartNew(o =>
                    {
                        var entry = (QueueWaitingEntry<HttpListenerContext>)o;
                        entry.NextHandler.Invoke(entry.Elem);

                        semaphore.Release();

                    }, contextEntry);
                else
                    Task.Factory.StartNew(o =>
                    {
                        var context = (HttpListenerContext)o; 
                        using (context.Response)
                        {
                            context.Response.StatusCode = 429;
                            context.Response.Close();
                        }
                        semaphore.Release();
                    }, contextEntry.Elem);
                //semaphore.Release();
            }
        }
        

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            queueHandleThread.Abort();
            queueHandleThread.Join();
            queue?.Dispose();
            semaphore?.Dispose();
        }

        private class QueueWaitingEntry<T>
        {
            public T Elem { get; }
            public Action<T> NextHandler { get; }
            public DateTime EnqueuedAt { get; }
            public QueueWaitingEntry(T elem, Action<T> nextHandler)
            {
                Elem = elem;
                NextHandler = nextHandler;
                EnqueuedAt = DateTime.Now;
            }
        }

    }
}
