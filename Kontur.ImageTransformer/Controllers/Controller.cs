using System;
using System.Net;

namespace Kontur.ImageTransformer.Controllers
{
    public abstract class Controller
    {
        public HttpListenerRequest Request { get; private set; }
        public HttpListenerResponse Response { get; private set; }
        protected bool Closed = false;

        public DateTime Created { get; private set; }

        protected Controller(HttpListenerRequest request, HttpListenerResponse response)
        {
            Created = DateTime.Now;
            Request = request;
            Response = response;
        }

        protected Controller(HttpListenerContext listenerContext)
        {
            Created = DateTime.Now;
            Request = listenerContext.Request;
            Response = listenerContext.Response;
        }

        protected void RefuseRequest()
        {
            Response.StatusCode = 429;
            if (!Closed)
                Response.Close();
            Closed = true;
        }

        public void RefuseRequestSafely()
        {
            using (Response)
                RefuseRequest();
        }

        public abstract void HandleRequest();

        protected void SendBadRequest()
        {
            Response.StatusCode = (int) HttpStatusCode.BadRequest;
            if (!Closed)
                Response.Close();
            Closed = true;
        }

        protected void SendNotFound()
        {
            Response.StatusCode = (int) HttpStatusCode.NotFound;
            if (!Closed)
                Response.Close();
            Closed = true;
        }

        protected void SendNoContent()
        {
            Response.StatusCode = (int) HttpStatusCode.NoContent;
            if (!Closed)
                Response.Close();
            Closed = true;
        }
    }
}