using System.Net;

namespace Kontur.ImageTransformer
{
    public abstract class Controller
    {
        public HttpListenerRequest Request { get; private set; }
        public HttpListenerResponse Response { get; private set; }
        protected bool closed = false;

        protected Controller(HttpListenerContext listenerContext)
        {
            Request = listenerContext.Request;
            Response = listenerContext.Response;
        }

        public abstract void HandleRequest();

        protected void SendBadRequest()
        {
            Response.StatusCode = (int) HttpStatusCode.BadRequest;
            if (!closed)
                Response.Close();
            closed = true;
        }

        protected void SendNotFound()
        {
            Response.StatusCode = (int) HttpStatusCode.NotFound;
            if (!closed)
                Response.Close();
            closed = true;
        }

        protected void SendNoContent()
        {
            Response.StatusCode = (int) HttpStatusCode.NoContent;
            if (!closed)
                Response.Close();
            closed = true;
        }
    }
}