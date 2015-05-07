using System.Collections.Generic;
using fake_rabbit.models;
using RabbitMQ.Client;

namespace fake_rabbit
{
    public class FakeConnectionFactory:ConnectionFactory
    {
        public IConnection Connection { get; private set; }
        public RabbitServer Server { get; private set; }

        public FakeConnectionFactory()
        {
            Server = new RabbitServer();
        }

        public FakeConnectionFactory WithConnection(IConnection connection)
        {
            Connection = connection;
            return this;
        }

        public FakeConnectionFactory WithRabbitNode(RabbitServer server)
        {
            Server = server;
            return this;
        }

        public FakeConnection UnderlyingConnection
        {
            get { return (FakeConnection) Connection; }
        }

        public List<FakeModel> UnderlyingModel
        {
            get
            {
                var connection = UnderlyingConnection;
                if (connection== null)
                    return null;

                return connection.Models;
            }
        }

        public override IConnection CreateConnection()
        {
            if(Connection == null)
                Connection = new FakeConnection(Server);

            return Connection;
        }
    }
}