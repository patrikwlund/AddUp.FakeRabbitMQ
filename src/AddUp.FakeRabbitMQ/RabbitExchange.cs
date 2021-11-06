using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AddUp.RabbitMQ.Fakes
{
    internal sealed class RabbitExchange
    {
        private readonly IBindingMatcher matcher;
        private readonly RabbitServer server;

        public RabbitExchange(string type, RabbitServer rabbitServer)
        {
            Type = type;
            server = rabbitServer;
            matcher = BindingMatcherFactory.Create(type);

            Messages = new ConcurrentQueue<RabbitMessage>();
            Bindings = new ConcurrentDictionary<string, RabbitExchangeQueueBinding>();
            Arguments = new Dictionary<string, object>();
        }

        public string Type { get; set; }
        public ConcurrentQueue<RabbitMessage> Messages { get; }
        public ConcurrentDictionary<string, RabbitExchangeQueueBinding> Bindings { get; }
        public IDictionary<string, object> Arguments { get; set; }
        public string Name { get; set; }
        public bool IsDurable { get; set; }
        public bool AutoDelete { get; set; }

        public void PublishMessage(RabbitMessage message)
        {
            Messages.Enqueue(message);

            var matchingBindings = string.IsNullOrWhiteSpace(message.RoutingKey) ?
                Bindings.Values :
                Bindings.Values.Where(b => matcher.Matches(message.RoutingKey, b.RoutingKey));

            if (matchingBindings.Any())
            {
                foreach (var binding in matchingBindings)
                    binding.Queue.PublishMessage(message);
                return;
            }

            // Alternate Exchange support
            if (Arguments == null) 
                return;

            if (!Arguments.TryGetValue("alternate-exchange", out var alternateExchangeName) || alternateExchangeName == null)
                return;

            if (!server.Exchanges.TryGetValue(alternateExchangeName.ToString(), out var alternateExchange))
                return;
            
            alternateExchange.PublishMessage(message);
        }
    }
}