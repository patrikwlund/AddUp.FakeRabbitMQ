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

        public ConcurrentQueue<dynamic> Messages = new ConcurrentQueue<dynamic>();
        public ConcurrentDictionary<string,ExchangeQueueBinding> Bindings = new ConcurrentDictionary<string,ExchangeQueueBinding>();

        public void PublishMessage(dynamic message, string routingKey)
        {
            this.Messages.Enqueue(message);

            if (string.IsNullOrWhiteSpace(routingKey))
            {
                foreach (var binding in Bindings)
                {
                    binding.Value.Queue.Messages.Enqueue(message);
                }
            }
            else
            {
                var matchingBindings = Bindings
                    .Values
                    .Where(b => b.RoutingKey == routingKey);

                foreach (var binding in matchingBindings)
                {
                    binding.Queue.Messages.Enqueue(message);
                }
            }
        }
    }
}