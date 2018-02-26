using System.Net;
using Ether.Network.Packets;

namespace Kontur.ImageTransformer.Controllers
{
    public abstract class Controller
    {
        public NetPacket NetPacket { get; }

        //public HttpListenerRequest Request { get; private set; }
        //public HttpListenerResponse Response { get; private set; }
        //protected bool Closed = false;

        protected Controller(HttpListenerContext listenerContext)
        {
            Request = listenerContext.Request;
            Response = listenerContext.Response;
        }

        protected Controller(NetPacket netPacket)
        {
            NetPacket = netPacket;
        }

        protected void RefuseRequest()
        {
            NetPacket.Write("HTTP/1.1 429");
        }

        public void RefuseRequestSafely()
        {
            RefuseRequest();
        }
        public abstract void HandleRequest();

        protected void SendBadRequest()
        {
            NetPacket.Write("HTTP/1.1  400 Bad Request");
        }

        protected void SendNotFound()
        {
            NetPacket.Write("HTTP/1.1  404 Not Found");
        }

        protected void SendNoContent()
        {
            NetPacket.Write("HTTP/1.1  204 No Content");
        }
    }
}