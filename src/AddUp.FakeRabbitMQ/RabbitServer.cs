using System.Collections.Concurrent;
using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes
{
    public sealed class RabbitServer
    {
        /// <summary>
        /// By default, queues are non-blocking. Pass <c>true</c> to <paramref name="blockingDeliveryMode"/> to use blocking queues
        /// Using blocking queues is quite different from what a real RabbitMQ setup would do, but still, it can greatly simplify unit tests.
        /// </summary>
        /// <param name="blockingDeliveryMode"></param>
        public RabbitServer(bool blockingDeliveryMode = false)
        {
            BlockingDeliveryMode = blockingDeliveryMode;
            Exchanges = new ConcurrentDictionary<string, RabbitExchange>();
            // Pre-declare the default exchange.
            // https://www.rabbitmq.com/tutorials/amqp-concepts.html#exchange-default
            Exchanges[""] = new RabbitExchange(ExchangeType.Direct, this)
            {
                Name = "",
                Arguments = null,
                AutoDelete = false,
                IsDurable = true,
            };
            Queues = new ConcurrentDictionary<string, RabbitQueue>();
        }

        public ConcurrentDictionary<string, RabbitExchange> Exchanges { get; }
        public ConcurrentDictionary<string, RabbitQueue> Queues { get; }

        /// <summary>
        /// If true, deliveries to consumers will execute in a blocking manner, meaning the publish will not
        /// finish until the message has reached all registered consumers.
        /// </summary>
        public bool BlockingDeliveryMode { get; }

        public void Reset()
        {
            Exchanges.Clear();
            Queues.Clear();
        }
    }
}