using RabbitMQ.Client;

namespace fake_rabbit
{
    public class InMemoryConnectionFactory:ConnectionFactory
    {
        public override IConnection CreateConnection()
        {
            return new InMemoryConnection();
        }
    }
}