using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace RabbitMQ.Fakes
{
    public sealed class FakeConnectionFactory:ConnectionFactory
    {
        public FakeConnectionFactory():this(new RabbitServer()) { }
        public FakeConnectionFactory(RabbitServer rabbitServer) => Server = rabbitServer ?? throw new ArgumentNullException(nameof(rabbitServer));

        public IConnection Connection { get; private set; }
        public RabbitServer Server { get; private set; }

        internal FakeConnection UnderlyingConnection => (FakeConnection)Connection;

        internal List<FakeModel> UnderlyingModel
        {
            get
            {
                var connection = UnderlyingConnection;
                return connection?.Models;
            }
        }

        public FakeConnectionFactory WithConnection(IConnection connection)
        {
            Connection = connection;
            return this;
        }

        public FakeConnectionFactory WithRabbitServer(RabbitServer server)
        {
            Server = server;
            return this;
        }        

        public override IConnection CreateConnection()
        {
            if(Connection == null)
                Connection = new FakeConnection(Server);

            return Connection;
        }
    }
}