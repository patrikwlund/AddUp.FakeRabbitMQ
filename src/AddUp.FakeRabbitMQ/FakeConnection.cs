using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace AddUp.RabbitMQ.Fakes
{
    internal sealed class FakeConnection : IConnection
    {
        private readonly RabbitServer server;

        public FakeConnection(RabbitServer rabbitServer) : this(rabbitServer, null) { }
        public FakeConnection(RabbitServer rabbitServer, string name)
        {
            server = rabbitServer ?? throw new ArgumentNullException(nameof(rabbitServer));
            ClientProvidedName = name ?? string.Empty;
            Models = new List<IModel>();
        }

#pragma warning disable 67
        public event EventHandler<CallbackExceptionEventArgs> CallbackException;
        //public event EventHandler<EventArgs> RecoverySucceeded; // REMOVED
        //public event EventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryError; // REMOVED
        public event EventHandler<ConnectionBlockedEventArgs> ConnectionBlocked;
        public event EventHandler<ShutdownEventArgs> ConnectionShutdown;
        public event EventHandler<EventArgs> ConnectionUnblocked;
#pragma warning restore 67

        public string ClientProvidedName { get; } // OK
        //public List<IModel> Models { get; private set; } // REMOVED
        //public EndPoint LocalEndPoint { get; set; } // REMOVED
        //public EndPoint RemoteEndPoint { get; set; } // REMOVED
        public int LocalPort { get; /*set;*/ } // OK
        public int RemotePort { get; /*set;*/ } // OK
        public AmqpTcpEndpoint Endpoint { get; /*set;*/ } // OK
        public IProtocol Protocol { get; /*set;*/ } // OK
        //public ConsumerWorkService ConsumerWorkService { get; } // REMOVED
        public ushort ChannelMax { get; /*set;*/ } // OK
        public uint FrameMax { get; /*set;*/ } // OK
        //public ushort Heartbeat { get; set; } // OLD
        public TimeSpan Heartbeat { get; /*set;*/ } // NEW                
        public AmqpTcpEndpoint[] KnownHosts { get; /*set;*/ } // OK        
        public ShutdownEventArgs CloseReason { get; private set; } // OK
        public bool IsOpen => CloseReason == null; // OK
        //public bool AutoClose { get; set; } // REMOVED
        public IDictionary<string, object> ServerProperties { get; } = new Dictionary<string, object>(); // OK
        public IList<ShutdownReportEntry> ShutdownReport { get; /*set;*/ } = new List<ShutdownReportEntry>(); // OK
        public IDictionary<string, object> ClientProperties { get; } = new Dictionary<string, object>(); // OK

        private List<IModel> Models { get; }

        internal List<IModel> GetModelsForUnitTests() => Models;

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

        public void UpdateSecret(string newSecret, string reason)
        {
            // Fake implementation. Nothing to do here.
        }

        // Close and Abort (implementation inspired by RabbitMQ.Client)

        public void Abort() => Abort(Timeout.InfiniteTimeSpan);
        public void Abort(TimeSpan timeout) => Abort(200, "Connection close forced", timeout);
        public void Abort(ushort reasonCode, string reasonText) => Abort(reasonCode, reasonText, Timeout.InfiniteTimeSpan);
        public void Abort(ushort reasonCode, string reasonText, TimeSpan timeout) =>
            Close(new ShutdownEventArgs(ShutdownInitiator.Application, reasonCode, reasonText), abort: true);

        public void Close() => Close(200, "Goodbye", Timeout.InfiniteTimeSpan);
        public void Close(TimeSpan timeout) => Close(200, "Goodbye", timeout);
        public void Close(ushort reasonCode, string reasonText) => Close(reasonCode, reasonText, Timeout.InfiniteTimeSpan);
        public void Close(ushort reasonCode, string reasonText, TimeSpan timeout) =>
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