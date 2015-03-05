using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace fake_rabbit.models
{
    public class Exchange
    {
        public string Name { get; set; } 
        public string Type { get; set; } 
        public bool IsDurable { get; set; } 
        public bool AutoDelete { get; set; } 
        public IDictionary Arguments = new Dictionary<string, object>();

        public ConcurrentQueue<RabbitMessage> Messages = new ConcurrentQueue<RabbitMessage>();
        public ConcurrentDictionary<string,ExchangeQueueBinding> Bindings = new ConcurrentDictionary<string,ExchangeQueueBinding>();

        public void PublishMessage(RabbitMessage message)
        {
            this.Messages.Enqueue(message);

            if (string.IsNullOrWhiteSpace(message.RoutingKey))
            {
                foreach (var binding in Bindings)
                {
                    binding.Value.Queue.PublishMessage(message);
                }
            }
            else
            {
                var matchingBindings = Bindings
                    .Values
                    .Where(b => b.RoutingKey == message.RoutingKey);

                foreach (var binding in matchingBindings)
                {
                    binding.Queue.PublishMessage(message);
                }
            }
        }
    }
}