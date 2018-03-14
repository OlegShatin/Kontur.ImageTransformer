using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PipelineNet.Middleware;

namespace Kontur.ImageTransformer.Infrastruct
{
    public class LoadThrottlerMiddleware : IMiddleware<HttpListenerContext>, IDisposable
    {
        //
        private const int WaitingLimitMs = 200;
        private const int WaitingBufferMs = 800 - WaitingLimitMs;

        private readonly int maxBufferizationQueueLength = 350 * Environment.ProcessorCount;
        private int queueLength = 0;

        private static readonly int maxParallelTasks = Environment.ProcessorCount * 25;
        private bool disposed;

        public LoadThrottlerMiddleware()
        {
            ThreadPool.SetMaxThreads(maxParallelTasks, 1000);
        }

        public void Run(HttpListenerContext context, Action<HttpListenerContext> next)
        {
            
            Interlocked.Increment(ref queueLength);
            
            Task.Factory.StartNew(o =>
            {
                var contextEntry = (QueueWaitingEntry<HttpListenerContext>) o;
                if ((DateTime.Now - contextEntry.EnqueuedAt).Milliseconds < WaitingLimitMs +
                    (queueLength > maxParallelTasks ? WaitingBufferMs * queueLength / maxBufferizationQueueLength : 0))

                {
                    contextEntry.NextHandler.Invoke(contextEntry.Elem);
                }
                else

                {
                    using (contextEntry.Elem.Response)
                    {
                        contextEntry.Elem.Response.StatusCode = 429;
                        contextEntry.Elem.Response.Close();
                    }
                }

                Interlocked.Decrement(ref queueLength);
            }, new QueueWaitingEntry<HttpListenerContext>(context, next));
        }


        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
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