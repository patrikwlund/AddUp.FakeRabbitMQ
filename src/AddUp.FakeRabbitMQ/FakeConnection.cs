using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace AddUp.RabbitMQ.Fakes
{
    internal sealed class FakeConnection : IConnection
    {
        private readonly RabbitServer server;

        public FakeConnection(RabbitServer rabbitServer) : this(rabbitServer, "") { }
        public FakeConnection(RabbitServer rabbitServer, string name)
        {
            server = rabbitServer ?? throw new ArgumentNullException(nameof(rabbitServer));
            ClientProvidedName = name ?? string.Empty;
            Models = new List<IModel>();
        }

#pragma warning disable 67
        public event EventHandler<CallbackExceptionEventArgs> CallbackException;
        public event EventHandler<EventArgs> RecoverySucceeded;
        public event EventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryError;
        public event EventHandler<ConnectionBlockedEventArgs> ConnectionBlocked;
        public event EventHandler<ShutdownEventArgs> ConnectionShutdown;
        public event EventHandler<EventArgs> ConnectionUnblocked;
#pragma warning restore 67

        public string ClientProvidedName { get; }
        public List<IModel> Models { get; private set; }
        public EndPoint LocalEndPoint { get; set; }
        public EndPoint RemoteEndPoint { get; set; }
        public int LocalPort { get; set; }
        public int RemotePort { get; set; }
        public AmqpTcpEndpoint Endpoint { get; set; }
        public IProtocol Protocol { get; set; }
        public ConsumerWorkService ConsumerWorkService { get; }
        public ushort ChannelMax { get; set; }
        public uint FrameMax { get; set; }
        public ushort Heartbeat { get; set; }
                
        public AmqpTcpEndpoint[] KnownHosts { get; set; }
        
        public ShutdownEventArgs CloseReason { get; private set; }
        public bool IsOpen => CloseReason == null;
        public bool AutoClose { get; set; }

        public IDictionary<string, object> ServerProperties { get; } = new Dictionary<string, object>();
        public IList<ShutdownReportEntry> ShutdownReport { get; set; } = new List<ShutdownReportEntry>();
        public IDictionary<string, object> ClientProperties { get; } = new Dictionary<string, object>();

        public void Dispose()
        {
            if (IsOpen) Abort(); // Abort rather than Close because we do not want Dispose to throw
        }

        public IModel CreateModel()
        {
            var model = new FakeModel(server);
            model.ModelShutdown += OnModelShutdown;
            Models.Add(model);

            return model;
        }
        
        public void HandleConnectionBlocked(string reason)
        {
            // Fake implementation. Nothing to do here.
        }

        public void HandleConnectionUnblocked()
        {
            // Fake implementation. Nothing to do here.
        }

        // Close and Abort (implementation inspired by RabbitMQ.Client)

        public void Abort() => Abort(-1);
        public void Abort(int timeout) => Abort(200, "Connection close forced", timeout);
        public void Abort(ushort reasonCode, string reasonText) => Abort(reasonCode, reasonText, -1);
        public void Abort(ushort reasonCode, string reasonText, int timeout) =>
            Close(new ShutdownEventArgs(ShutdownInitiator.Application, reasonCode, reasonText), abort: true);

        public void Close() => Close(200, "Goodbye", -1);
        public void Close(int timeout) => Close(200, "Goodbye", timeout);
        public void Close(ushort reasonCode, string reasonText) => Close(reasonCode, reasonText, -1);
        public void Close(ushort reasonCode, string reasonText, int timeout) =>
            Close(new ShutdownEventArgs(ShutdownInitiator.Application, reasonCode, reasonText), abort: false);

        // Not part of the original implementation. Required by FakeConnectionFactory
        // NB: does not recreate the models
        internal void ForceOpen() => CloseReason = null;

        private void Close(ShutdownEventArgs reason, bool abort)
        {
            try
            {
                if (!IsOpen)
                    throw new AlreadyClosedException(reason);

                CloseReason = reason;
                var modelsCopy = Models.ToList();
                modelsCopy.ForEach(m =>
                {
                    if (abort)
                        m.Abort();
                    else
                        m.Close();
                });

                ConnectionShutdown?.Invoke(this, reason);
            }
            catch
            {
                if (!abort) throw;
            }
        }

        private void OnModelShutdown(object sender, ShutdownEventArgs e)
        {
            var model = (IModel)sender;
            model.ModelShutdown -= OnModelShutdown;
            _ = Models.Remove(model);
        }
    }
}