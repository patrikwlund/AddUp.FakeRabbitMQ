using System.Collections.Concurrent;

namespace RabbitMQ.Fakes
{
    public sealed class RabbitServer
    {
        internal ConcurrentDictionary<string, RabbitExchange> Exchanges { get; } = new ConcurrentDictionary<string, RabbitExchange>();
        internal ConcurrentDictionary<string, RabbitQueue> Queues { get; } = new ConcurrentDictionary<string, RabbitQueue>();
        
        public void Reset()
        {
            Exchanges.Clear();
            Queues.Clear();
        }
    }
}