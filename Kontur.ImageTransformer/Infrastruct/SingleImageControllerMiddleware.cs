using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Kontur.ImageTransformer.Controllers;
using PipelineNet.Middleware;

namespace Kontur.ImageTransformer.Infrastruct
{
    public class SingleImageControllerMiddleware : IMiddleware<HttpListenerContext>
    {
        public void Run(HttpListenerContext context, Action<HttpListenerContext> next)
        {
            (new ImageController(context)).HandleRequest();
            next.Invoke(context);
        }
    }

    
}
