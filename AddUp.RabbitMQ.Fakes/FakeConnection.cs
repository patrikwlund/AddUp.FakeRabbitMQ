using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Fakes
{
    internal sealed class FakeConnection : IConnection
    {
        private readonly RabbitServer server;

        public FakeConnection(RabbitServer rabbitServer)
        {
            server = rabbitServer ?? throw new ArgumentNullException(nameof(rabbitServer));
            Models = new List<FakeModel>();
        }

        public event EventHandler<CallbackExceptionEventArgs> CallbackException;
        public event EventHandler<EventArgs> RecoverySucceeded;
        public event EventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryError;
        public event EventHandler<ConnectionBlockedEventArgs> ConnectionBlocked;
        public event EventHandler<ShutdownEventArgs> ConnectionShutdown;
        public event EventHandler<EventArgs> ConnectionUnblocked;

        public List<FakeModel> Models { get; private set; }
        public EndPoint LocalEndPoint { get; set; }
        public EndPoint RemoteEndPoint { get; set; }
        public int LocalPort { get; set; }
        public int RemotePort { get; set; }
        public AmqpTcpEndpoint Endpoint { get; set; }
        public IProtocol Protocol { get; set; }
        public string ClientProvidedName { get; }
        public ConsumerWorkService ConsumerWorkService { get; }
        public ushort ChannelMax { get; set; }        
        public uint FrameMax { get; set; }
        public ushort Heartbeat { get; set; }
        public IDictionary ClientProperties { get; set; }
        public IDictionary ServerProperties { get; set; }
        public AmqpTcpEndpoint[] KnownHosts { get; set; }
        public ShutdownEventArgs CloseReason { get; set; }
        public bool IsOpen { get; set; }
        public bool AutoClose { get; set; }
        public IList ShutdownReport { get; set; }
        IDictionary<string, object> IConnection.ServerProperties => throw new NotImplementedException();
        IList<ShutdownReportEntry> IConnection.ShutdownReport => throw new NotImplementedException();
        IDictionary<string, object> IConnection.ClientProperties => throw new NotImplementedException();

        public void Dispose()
        {
            // Fake implementation. Nothing to do here.
        }

        public IModel CreateModel()
        {
            var model = new FakeModel(server);
            Models.Add(model);

            return model;
        }

        public void Close() => Close(1, null, 0);
        public void Close(ushort reasonCode, string reasonText) => Close(reasonCode, reasonText, 0);
        public void Close(int timeout) => Close(1, null, timeout);

        public void Close(ushort reasonCode, string reasonText, int timeout)
        {
            IsOpen = false;
            CloseReason = new ShutdownEventArgs(ShutdownInitiator.Library, reasonCode, reasonText);

            Models.ForEach(m => m.Close());
        }

        public void Abort() => Abort(1, null, 0);
        public void Abort(int timeout) => Abort(1, null, timeout);
        public void Abort(ushort reasonCode, string reasonText) => Abort(reasonCode, reasonText, 0);
        public void Abort(ushort reasonCode, string reasonText, int timeout)
        {
            IsOpen = false;
            CloseReason = new ShutdownEventArgs(ShutdownInitiator.Library, reasonCode, reasonText);

            Models.ForEach(m => m.Abort());
        }

        public void HandleConnectionBlocked(string reason)
        {
            // Fake implementation. Nothing to do here.
        }

        public void HandleConnectionUnblocked()
        {
            // Fake implementation. Nothing to do here.
        }
    }
}