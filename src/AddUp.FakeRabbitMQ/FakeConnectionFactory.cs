using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes
{
    public sealed class FakeConnectionFactory : IConnectionFactory
    {
        private readonly RabbitServer server;

        public FakeConnectionFactory() : this(new RabbitServer()) { }
        public FakeConnectionFactory(RabbitServer rabbitServer) =>
            server = rabbitServer ?? throw new ArgumentNullException(nameof(rabbitServer));

        public IDictionary<string, object> ClientProperties { get; set; }
        public string Password { get; set; }
        public ICredentialsProvider CredentialsProvider { get; set; }
        public ICredentialsRefresher CredentialsRefresher { get; set; }
        public ushort RequestedChannelMax { get; set; }
        public uint RequestedFrameMax { get; set; }
        public TimeSpan RequestedHeartbeat { get; set; }
        [Obsolete("Currently a no-op. UseBackgroundThreadsForIO will be removed in version 7")] // See https://github.com/rabbitmq/rabbitmq-dotnet-client/pull/1388
        public bool UseBackgroundThreadsForIO { get; set; }
        public string UserName { get; set; }
        public string VirtualHost { get; set; }
        public Uri Uri { get; set; }
        public string ClientProvidedName { get; set; }
        public TimeSpan HandshakeContinuationTimeout { get; set; }
        public TimeSpan ContinuationTimeout { get; set; }

        public IAuthMechanismFactory AuthMechanismFactory(IList<string> mechanismNames) =>
            new PlainMechanismFactory();

        public IConnection CreateConnection() => CreateConnection(ClientProvidedName);
        public IConnection CreateConnection(IList<string> hostnames) => CreateConnection(hostnames, ClientProvidedName);
        public IConnection CreateConnection(IList<string> hostnames, string clientProvidedName) => CreateConnection(clientProvidedName);
        public IConnection CreateConnection(IList<AmqpTcpEndpoint> endpoints) => CreateConnection(endpoints, ClientProvidedName);
        public IConnection CreateConnection(IList<AmqpTcpEndpoint> endpoints, string clientProvidedName) => CreateConnection(clientProvidedName);

        public IConnection CreateConnection(string clientProvidedName)
        {
            return new FakeConnection(server, clientProvidedName);
        }
    }
}
