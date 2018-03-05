using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kontur.ImageTransformer.Controllers;
using Kontur.ImageTransformer.Infrastruct;
using NLog;
using NLog.Targets;
using PipelineNet;
using PipelineNet.Middleware;
using PipelineNet.MiddlewareResolver;
using PipelineNet.Pipelines;

namespace Kontur.ImageTransformer
{
    public class AsyncHttpServer : IDisposable
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private readonly HttpListener listener;
        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;
        private LoadThrottlerMiddleware loadThrottler;
        private IPipeline<HttpListenerContext> pipeline;

        public AsyncHttpServer()
        {
            listener = new HttpListener();
            loadThrottler = new LoadThrottlerMiddleware();
            var mwResolver = new SingletoneMiddlewaresResolver();

            mwResolver.AddInstance(loadThrottler);
            mwResolver.AddInstance(new SingleImageControllerMiddleware());

            pipeline = new Pipeline<HttpListenerContext>(mwResolver)
                .Add<LoadThrottlerMiddleware>()
                .Add<SingleImageControllerMiddleware>();
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
            loadThrottler.Dispose();

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
                        pipeline.Execute(listenerContext);

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
        
        
        
        
    }
    public class SingletoneMiddlewaresResolver : IMiddlewareResolver
    {
        private Dictionary<Type, object> singletones = new Dictionary<Type, object>();
        public object Resolve(Type type)
        {
            return singletones[type];
        }

        public void AddInstance<TParam>(IMiddleware<TParam> middleware)
        {
            singletones.Add(middleware.GetType(), middleware);
        }
    }
    
}