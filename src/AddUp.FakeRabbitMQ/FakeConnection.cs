using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            Models = new List<IChannel>();
        }

#pragma warning disable 67
        public event AsyncEventHandler<CallbackExceptionEventArgs> CallbackExceptionAsync;
        public event AsyncEventHandler<ConnectionBlockedEventArgs> ConnectionBlockedAsync;
        public event AsyncEventHandler<ShutdownEventArgs> ConnectionShutdownAsync;
        public event AsyncEventHandler<AsyncEventArgs> ConnectionUnblockedAsync;
        // The events below come from IAutorecoveringConnection; everything else is from IConnection
        public event AsyncEventHandler<AsyncEventArgs> RecoverySucceededAsync;
        public event AsyncEventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryErrorAsync;
        public event AsyncEventHandler<ConsumerTagChangedAfterRecoveryEventArgs> ConsumerTagChangeAfterRecoveryAsync;
        public event AsyncEventHandler<QueueNameChangedAfterRecoveryEventArgs> QueueNameChangedAfterRecoveryAsync;
        public event AsyncEventHandler<RecoveringConsumerEventArgs> RecoveringConsumerAsync;
#pragma warning restore 67

        public string ClientProvidedName { get; }
        public int LocalPort { get; }
        public int RemotePort { get; }
        public AmqpTcpEndpoint Endpoint { get; }
        public IProtocol Protocol { get; }
        public ushort ChannelMax { get; }
        public uint FrameMax { get; }
        public TimeSpan Heartbeat { get; }
        public AmqpTcpEndpoint[] KnownHosts { get; }
        public ShutdownEventArgs CloseReason { get; private set; }
        public bool IsOpen => CloseReason == null;
        public IDictionary<string, object> ServerProperties { get; } = new Dictionary<string, object>();
        public IEnumerable<ShutdownReportEntry> ShutdownReport { get; } = new List<ShutdownReportEntry>();
        public IDictionary<string, object> ClientProperties { get; } = new Dictionary<string, object>();

        private List<IChannel> Models { get; }

        internal List<IChannel> GetModelsForUnitTests() => Models;

        public void Dispose()
        {
            if (IsOpen) this.AbortAsync().GetAwaiter().GetResult(); // Abort rather than Close because we do not want Dispose to throw
        }

        public async ValueTask DisposeAsync()
        {
            if (IsOpen) await this.AbortAsync(); // Abort rather than Close because we do not want Dispose to throw
        }

        public Task<IChannel> CreateChannelAsync(CreateChannelOptions options = default, CancellationToken cancellationToken = default)
        {
            var model = new FakeModel(server);
            model.ChannelShutdownAsync += OnChannelShutdown;
            Models.Add(model);

            return Task.FromResult((IChannel)model);
        }

        public Task UpdateSecretAsync(string newSecret, string reason, CancellationToken cancellationToken = default)
        {
            // Fake implementation. Nothing to do here.
            return Task.CompletedTask;
        }

        public async Task CloseAsync(ushort reasonCode, string reasonText, TimeSpan timeout, bool abort,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var reason = new ShutdownEventArgs(ShutdownInitiator.Application, reasonCode, reasonText);

                if (!IsOpen)
                    throw new AlreadyClosedException(reason);

                CloseReason = reason;
                var modelsCopy = Models.ToList();
                modelsCopy.ForEach(m =>
                {
                    if (abort)
                        m.AbortAsync();
                    else
                        m.CloseAsync();
                });

                if (ConnectionShutdownAsync is not null)
                {
                    await ConnectionShutdownAsync.Invoke(this, reason);
                }
            }
            catch
            {
                if (!abort) throw;
            }
        }

        private Task OnChannelShutdown(object sender, ShutdownEventArgs e)
        {
            var model = (IChannel)sender;
            model.ChannelShutdownAsync -= OnChannelShutdown;
            _ = Models.Remove(model);

            return Task.CompletedTask;
        }
    }
}
