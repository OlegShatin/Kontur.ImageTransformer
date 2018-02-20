using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kontur.ImageTransformer.Controllers;
using NLog;
using NLog.Targets;

namespace Kontur.ImageTransformer
{
    public class AsyncHttpServer
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private HttpListener listener;

        private Func<HttpListenerRequest, HttpListenerResponse, Controller> controllerFactory =
            (req, resp) => new ImageController(req, resp);
        
        readonly int accepts;

        /// <summary>
        /// Creates an asynchronous HTTP host.
        /// </summary>
        /// <param name="handler">Handler to serve requests with</param>
        /// <param name="accepts">
        /// Higher values mean more connections can be maintained yet at a much slower average response time; fewer connections will be rejected.
        /// Lower values mean less connections can be maintained yet at a much faster average response time; more connections will be rejected.
        /// </param>
        public AsyncHttpServer(Func<HttpListenerRequest, HttpListenerResponse, Controller> controllerFactory = null, int accepts = 1)
        {
            if (controllerFactory != null)
                this.controllerFactory = controllerFactory;
            listener = new HttpListener();
            // Multiply by number of cores:
            this.accepts = accepts * Environment.ProcessorCount;
        }

        

        public List<string> Prefixes
        {
            get { return listener.Prefixes.ToList(); }
        }

        

        public void Run(params string[] uriPrefixes)
        {
            

            listener.IgnoreWriteExceptions = true;

            // Add the server bindings:
            foreach (var prefix in uriPrefixes)
                listener.Prefixes.Add(prefix);

            Task.Run(async () =>
            {
                
                

                try
                {
                    // Start the HTTP listener:
                    listener.Start();
                }
                catch (HttpListenerException hlex)
                {
                    Console.Error.WriteLine(hlex.Message);
                    return;
                }

                // Accept connections:
                // Higher values mean more connections can be maintained yet at a much slower average response time; fewer connections will be rejected.
                // Lower values mean less connections can be maintained yet at a much faster average response time; more connections will be rejected.
                var sem = new Semaphore(accepts, accepts);

                while (true)
                {
                    sem.WaitOne();

#pragma warning disable 4014
                    listener.GetContextAsync().ContinueWith(async (t) =>
                    {
                        

                        try
                        {
                            sem.Release();

                            var ctx = await t;
                            await ProcessListenerContext(ctx);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex);
                        }

                        
                    });
#pragma warning restore 4014
                }
            }).Wait();
        }

        async Task ProcessListenerContext(HttpListenerContext listenerContext)
        {
            try
            {
                // Get the response action to take:
                var controller = controllerFactory.Invoke(listenerContext.Request, listenerContext.Response);
                if (controller != null)
                {
                    // Take the action and await its completion:
                    await  Task.Factory.StartNew(() => DecideAndHandle(controller));
                }
            }
            catch (HttpListenerException e)
            {
                logger.Error(e);
            }
            catch (Exception ex)
            {
                // TODO: better exception handling
                logger.Error(ex.ToString());
            }
        }
        TimeSpan waitLimit = TimeSpan.FromMilliseconds(300);
        void DecideAndHandle(Controller controller)
        {
            if (DateTime.Now - controller.Created < waitLimit)
                controller.HandleRequest();
            else
                controller.RefuseRequestSafely();
        }
    }
}