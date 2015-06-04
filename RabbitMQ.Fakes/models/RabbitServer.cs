using System.Collections.Concurrent;

namespace RabbitMQ.Fakes.models
{
    public class RabbitServer
    {
        public ConcurrentDictionary<string, Exchange> Exchanges = new ConcurrentDictionary<string, Exchange>();
        public ConcurrentDictionary<string, models.Queue> Queues = new ConcurrentDictionary<string, models.Queue>();


        public void Reset()
        {
            Exchanges.Clear();
            Queues.Clear();
        }
    }
}