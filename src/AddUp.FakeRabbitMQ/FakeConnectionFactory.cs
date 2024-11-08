using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes
{
    public sealed class FakeConnectionFactory(RabbitServer rabbitServer) : IConnectionFactory
    {
        private readonly RabbitServer server = rabbitServer ?? throw new ArgumentNullException(nameof(rabbitServer));

        public FakeConnectionFactory() : this(new RabbitServer()) { }

        public IDictionary<string, object> ClientProperties { get; set; }
        public string Password { get; set; }
        public ICredentialsProvider CredentialsProvider { get; set; }
        public ushort RequestedChannelMax { get; set; }
        public uint RequestedFrameMax { get; set; }
        public TimeSpan RequestedHeartbeat { get; set; }
        public string UserName { get; set; }
        public string VirtualHost { get; set; }
        public Uri Uri { get; set; }
        public string ClientProvidedName { get; set; }
        public TimeSpan HandshakeContinuationTimeout { get; set; }
        public TimeSpan ContinuationTimeout { get; set; }
        public ushort ConsumerDispatchConcurrency { get; set; }

        public IAuthMechanismFactory AuthMechanismFactory(IEnumerable<string> mechanismNames) =>
            new PlainMechanismFactory();

        public Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken = default) => CreateConnectionAsync(ClientProvidedName, cancellationToken);
        public Task<IConnection> CreateConnectionAsync(IEnumerable<string> hostnames, CancellationToken cancellationToken = default) => CreateConnectionAsync(hostnames, ClientProvidedName, cancellationToken);
        public Task<IConnection> CreateConnectionAsync(IEnumerable<string> hostnames, string clientProvidedName, CancellationToken cancellationToken = default) => CreateConnectionAsync(clientProvidedName, cancellationToken);
        public Task<IConnection> CreateConnectionAsync(IEnumerable<AmqpTcpEndpoint> endpoints, CancellationToken cancellationToken = default) => CreateConnectionAsync(endpoints, ClientProvidedName, cancellationToken);
        public Task<IConnection> CreateConnectionAsync(IEnumerable<AmqpTcpEndpoint> endpoints, string clientProvidedName, CancellationToken cancellationToken = default) => CreateConnectionAsync(clientProvidedName, cancellationToken);

        public Task<IConnection> CreateConnectionAsync(string clientProvidedName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult((IConnection)new FakeConnection(server, clientProvidedName));
        }
    }
}
