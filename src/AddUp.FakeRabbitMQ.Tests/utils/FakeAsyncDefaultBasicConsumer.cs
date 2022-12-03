using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes
{
    [ExcludeFromCodeCoverage]
    internal sealed class FakeAsyncDefaultBasicConsumer : AsyncDefaultBasicConsumer
    {
        public FakeAsyncDefaultBasicConsumer(IModel model) : base(model) { }

        public TaskCompletionSource<(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body)> LastDelivery { get; } = new TaskCompletionSource<(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body)>();

        public TaskCompletionSource<string> LastCancelOkConsumerTag { get; } = new TaskCompletionSource<string>();

        public override Task HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, ReadOnlyMemory<byte> body)
        {
            LastDelivery.SetResult((consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body.ToArray()));
            return base.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);
        }

        public override Task HandleBasicCancelOk(string consumerTag)
        {
            LastCancelOkConsumerTag.SetResult(consumerTag);
            return base.HandleBasicCancelOk(consumerTag);
        }
    }
}
