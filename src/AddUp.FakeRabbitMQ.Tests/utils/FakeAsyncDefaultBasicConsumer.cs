using System.Diagnostics.CodeAnalysis;
using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes;

[ExcludeFromCodeCoverage]
internal sealed class FakeAsyncDefaultBasicConsumer(IChannel channel) : AsyncDefaultBasicConsumer(channel)
{
    public TaskCompletionSource<(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IReadOnlyBasicProperties properties, byte[] body)> LastDelivery { get; } = new();

    public TaskCompletionSource<string> LastCancelOkConsumerTag { get; } = new();

    public override Task HandleBasicDeliverAsync(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IReadOnlyBasicProperties properties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default)
    {
        LastDelivery.SetResult((consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body.ToArray()));

        return base.HandleBasicDeliverAsync(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body, cancellationToken);
    }

    public override Task HandleBasicCancelOkAsync(string consumerTag, CancellationToken cancellationToken = default)
    {
        LastCancelOkConsumerTag.SetResult(consumerTag);
        return base.HandleBasicCancelOkAsync(consumerTag, cancellationToken);
    }
}
