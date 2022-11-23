using System.Collections.Concurrent;

namespace AddUp.RabbitMQ.Fakes
{
    public sealed class RabbitServer
    {
        public RabbitServer()
        {
            Exchanges = new ConcurrentDictionary<string, RabbitExchange>();
            Queues = new ConcurrentDictionary<string, RabbitQueue>();
        }

        public ConcurrentDictionary<string, RabbitExchange> Exchanges { get; }
        public ConcurrentDictionary<string, RabbitQueue> Queues { get; }

        public void Reset()
        {
            Exchanges.Clear();
            Queues.Clear();
        }
    }
}