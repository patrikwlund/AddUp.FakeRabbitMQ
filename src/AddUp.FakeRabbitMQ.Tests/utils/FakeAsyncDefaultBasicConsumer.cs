using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes
{
    [ExcludeFromCodeCoverage]
    internal sealed class FakeAsyncDefaultBasicConsumer : AsyncDefaultBasicConsumer
    {
        public FakeAsyncDefaultBasicConsumer(IModel model) : base(model) { }

        public (string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body) LastDelivery { get; private set; }

        public string LastCancelOkConsumerTag { get; private set; }

        public override Task HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body)
        {
            LastDelivery = (consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);
            return base.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);
        }

        public override Task HandleBasicCancelOk(string consumerTag)
        {
            LastCancelOkConsumerTag = consumerTag;
            return base.HandleBasicCancelOk(consumerTag);
        }
    }
}
