using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ether.Network;
using Ether.Network.Packets;
using Kontur.ImageTransformer.Controllers;
using NLog;
using NLog.Targets;

namespace Kontur.ImageTransformer
{
    public class AsyncHttpServer : NetServer<Conn>
    {
        public AsyncHttpServer() : base()
        {
            this.Configuration.Backlog = 100;
            this.Configuration.Port = 8080;
            this.Configuration.MaximumNumberOfConnections = 100;
            this.Configuration.Host = "169.254.6.29";
            this.Configuration.BufferSize = 110 * 1024;
        }
        protected override void Initialize()
        {
            
        }

        protected override void OnClientConnected(Conn connection)
        {
            
        }

        protected override void OnClientDisconnected(Conn connection)
        {
        }
    }

    public class Conn : NetConnection
    {
        public override void HandleMessage(NetPacketBase packet)
        {
            using (var resppacket = new NetPacket())
            {
                resppacket.Write("HTTP/1.1 200 OK");
                this.Send(resppacket);
            }
        }
    }
}