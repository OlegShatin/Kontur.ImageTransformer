using System.Net;
using SAEAHTTPD;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace Kontur.ImageTransformer.Controllers
{
    public abstract class Controller
    {
        public HttpRequest Request { get; }
        public HttpResponse Response { get; }


        protected Controller(HttpRequest request, HttpResponse response)
        {
            Request = request;
            Response = response;
        }

        protected void RefuseRequest()
        {
            Response.Status = SAEAHTTPD.HttpStatusCode.UseProxy;
        }

        public void RefuseRequestSafely()
        {
            RefuseRequest();
        }

        public abstract void HandleRequest();

        protected void SendBadRequest()
        {
            Response.Status = SAEAHTTPD.HttpStatusCode.BadRequest;
        }

        protected void SendNotFound()
        {
            Response.Status = SAEAHTTPD.HttpStatusCode.NotFound;
        }

        protected void SendNoContent()
        {
            Response.Status = SAEAHTTPD.HttpStatusCode.NoContent;
        }
    }
}