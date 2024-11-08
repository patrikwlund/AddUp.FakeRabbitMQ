using System.Collections.Concurrent;

namespace AddUp.RabbitMQ.Fakes
{
    public sealed class RabbitExchange
    {
        private readonly IBindingMatcher matcher;
        private readonly RabbitServer server;

        public RabbitExchange(string type, RabbitServer rabbitServer)
        {
            Type = type;
            server = rabbitServer;
            matcher = BindingMatcherFactory.Create(type);

            Messages = new ConcurrentQueue<RabbitMessage>();
            DroppedMessages = new ConcurrentQueue<RabbitMessage>();
            Bindings = new ConcurrentDictionary<string, RabbitExchangeQueueBinding>();
            Arguments = new Dictionary<string, object>();
        }

        public string Type { get; set; }
        public ConcurrentQueue<RabbitMessage> Messages { get; }
        public ConcurrentQueue<RabbitMessage> DroppedMessages { get; }
        public ConcurrentDictionary<string, RabbitExchangeQueueBinding> Bindings { get; }
        public IDictionary<string, object> Arguments { get; set; }
        public string Name { get; set; }
        public bool IsDurable { get; set; }
        public bool AutoDelete { get; set; }

        public async Task PublishMessage(RabbitMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Enqueue(message);

            var matchingBindings = string.IsNullOrWhiteSpace(message.RoutingKey) ?
                Bindings.Values :
                Bindings.Values.Where(b => matcher.Matches(message.RoutingKey, b.RoutingKey));

            if (matchingBindings.Any())
            {
                foreach (var binding in matchingBindings)
                {
                    await binding.Queue.PublishMessage(message, cancellationToken);
                }

                return;
            }

            // Alternate Exchange support
            if (Arguments == null)
            {
                DroppedMessages.Enqueue(message);
                return;
            }

            if (!Arguments.TryGetValue("alternate-exchange", out var alternateExchangeName) || alternateExchangeName == null)
            {
                DroppedMessages.Enqueue(message);
                return;
            }

            if (!server.Exchanges.TryGetValue(alternateExchangeName.ToString(), out var alternateExchange))
            {
                DroppedMessages.Enqueue(message);
                return;
            }

            await alternateExchange.PublishMessage(message, cancellationToken);
        }
    }
}
