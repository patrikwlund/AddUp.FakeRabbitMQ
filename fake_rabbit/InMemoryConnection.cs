using System.Collections.Generic;
using System.Net;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing.Impl;
using RabbitMQ.Client.Impl;

namespace fake_rabbit
{
    public class InMemoryConnection : IConnection
    {
        public EndPoint LocalEndPoint
        {
            get { throw new System.NotImplementedException(); }
        }

        public EndPoint RemoteEndPoint
        {
            get { throw new System.NotImplementedException(); }
        }

        public int LocalPort
        {
            get { throw new System.NotImplementedException(); }
        }

        public int RemotePort
        {
            get { throw new System.NotImplementedException(); }
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public IModel CreateModel()
        {
            return new InMemoryModel();
        }

        public void Close()
        {
            throw new System.NotImplementedException();
        }

        public void Close(ushort reasonCode, string reasonText)
        {
            throw new System.NotImplementedException();
        }

        public void Close(int timeout)
        {
            throw new System.NotImplementedException();
        }

        public void Close(ushort reasonCode, string reasonText, int timeout)
        {
            throw new System.NotImplementedException();
        }

        public void Abort()
        {
            throw new System.NotImplementedException();
        }

        public void Abort(ushort reasonCode, string reasonText)
        {
            throw new System.NotImplementedException();
        }

        public void Abort(int timeout)
        {
            throw new System.NotImplementedException();
        }

        public void Abort(ushort reasonCode, string reasonText, int timeout)
        {
            throw new System.NotImplementedException();
        }

        public void HandleConnectionBlocked(string reason)
        {
            throw new System.NotImplementedException();
        }

        public void HandleConnectionUnblocked()
        {
            throw new System.NotImplementedException();
        }

        public AmqpTcpEndpoint Endpoint
        {
            get { throw new System.NotImplementedException(); }
        }

        public IProtocol Protocol
        {
            get { throw new System.NotImplementedException(); }
        }

        public ushort ChannelMax
        {
            get { throw new System.NotImplementedException(); }
        }

        public uint FrameMax
        {
            get { throw new System.NotImplementedException(); }
        }

        public ushort Heartbeat
        {
            get { throw new System.NotImplementedException(); }
        }

        public IDictionary<string, object> ClientProperties
        {
            get { throw new System.NotImplementedException(); }
        }

        public IDictionary<string, object> ServerProperties
        {
            get { throw new System.NotImplementedException(); }
        }

        public AmqpTcpEndpoint[] KnownHosts
        {
            get { throw new System.NotImplementedException(); }
        }

        public ShutdownEventArgs CloseReason
        {
            get { throw new System.NotImplementedException(); }
        }

        public bool IsOpen
        {
            get { throw new System.NotImplementedException(); }
        }

        public bool AutoClose
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }

        public IList<ShutdownReportEntry> ShutdownReport
        {
            get { throw new System.NotImplementedException(); }
        }

        public event ConnectionShutdownEventHandler ConnectionShutdown;
        public event CallbackExceptionEventHandler CallbackException;
        public event ConnectionBlockedEventHandler ConnectionBlocked;
        public event ConnectionUnblockedEventHandler ConnectionUnblocked;
    }
}