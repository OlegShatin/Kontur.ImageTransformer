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

        private readonly TimeSpan waitingLimit = TimeSpan.FromMilliseconds(500);
        
        
        private static readonly int maxParallelTasks = Environment.ProcessorCount * 4;
        private bool disposed;

        public LoadThrottlerMiddleware()
        {
            ThreadPool.SetMaxThreads(maxParallelTasks, 1000);
            ThreadPool.SetMinThreads(maxParallelTasks, 1000);
        }
        
        public void Run(HttpListenerContext context, Action<HttpListenerContext> next)
        {
            Task.Factory.StartNew(o =>
            {
                var contextEntry = (QueueWaitingEntry<HttpListenerContext>)o;
                if (DateTime.Now - contextEntry.EnqueuedAt < waitingLimit)
                    
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
