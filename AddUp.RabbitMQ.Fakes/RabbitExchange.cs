using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RabbitMQ.Fakes
{
    internal sealed class RabbitExchange
    {
        public RabbitExchange()
        {
            Messages = new ConcurrentQueue<RabbitMessage>();
            Bindings = new ConcurrentDictionary<string, RabbitExchangeQueueBinding>();
            Arguments = new Dictionary<string, object>();
        }
        
        public ConcurrentQueue<RabbitMessage> Messages { get; }
        public ConcurrentDictionary<string, RabbitExchangeQueueBinding> Bindings { get; }
        public IDictionary<string, object> Arguments { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsDurable { get; set; }
        public bool AutoDelete { get; set; }

        public void PublishMessage(RabbitMessage message)
        {
            Messages.Enqueue(message);

            if (string.IsNullOrWhiteSpace(message.RoutingKey))
            {
                foreach (var binding in Bindings)
                    binding.Value.Queue.PublishMessage(message);
            }
            else
            {
                var matchingBindings = Bindings.Values.Where(b => b.RoutingKey == message.RoutingKey);
                foreach (var binding in matchingBindings)
                    binding.Queue.PublishMessage(message);
            }
        }
    }
}