using System.Diagnostics.CodeAnalysis;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace AddUp.RabbitMQ.Fakes.Repros;

// Ensures issues described in https://github.com/addupsolutions/AddUp.FakeRabbitMQ/issues/180 are fixed
[ExcludeFromCodeCoverage]
public sealed class BasicAckSemanticTests : IAsyncLifetime
{
    private const string exchange = "exchange";
    private const string queueName = "queue";
    private const string routingKey = "routing-key";
    private const int timeout = 1000;
    private readonly RabbitServer rabbitServer = new();
    private IConnection connection;
    private IChannel channel;

    public async Task InitializeAsync()
    {
        connection = await new FakeConnectionFactory(rabbitServer).CreateConnectionAsync();
        channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic);
        await channel.QueueDeclareAsync(queueName);
        await channel.QueueBindAsync(queueName, exchange, "#");
    }

    public async Task DisposeAsync()
    {
        await channel.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Ack_DeliveryTagsAreRespected()
    {
        using var allMessagesDelivered = new ManualResetEventSlim();
        var deliveryTags = new List<ulong>();

        // setup a basic consumer that stores delivery tags to the deliveryTags list
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, e) =>
        {
            deliveryTags.Add(e.DeliveryTag);
            if (deliveryTags.Count == 2)
                allMessagesDelivered.Set();

            return Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(queueName, false, consumer);

        // publish two messages
        await channel.BasicPublishAsync(exchange, routingKey, true, Encoding.UTF8.GetBytes("first"));
        await channel.BasicPublishAsync(exchange, routingKey, true, Encoding.UTF8.GetBytes("second"));

        // wait for both messages to be delivered
        allMessagesDelivered.Wait(timeout);

        // ack the **second** message, but not the first one
        await channel.BasicAckAsync(deliveryTags[1], false);

        // asserts that only one message is still queued
        Assert.True(rabbitServer.Queues[queueName].MessageCount == 1, "Only one message is still queued");

        // asserts that the remaining message in queue is the first one, since we acked the second
        rabbitServer.Queues[queueName].TryPeekForUnitTests(out var pendingMessage);
        Assert.True(Encoding.UTF8.GetString(pendingMessage.Body) == "first", "The remaining message in queue is the first one");
    }

    [Fact]
    public async Task Ack_Multiple()
    {
        using var allMessagesDelivered = new ManualResetEventSlim();
        var deliveryTags = new List<ulong>();

        // setup a basic consumer that stores delivery tags to the deliveryTags list
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, e) =>
        {
            deliveryTags.Add(e.DeliveryTag);
            if (deliveryTags.Count == 2)
                allMessagesDelivered.Set();

            return Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(queueName, false, consumer);

        // publish two messages
        await channel.BasicPublishAsync(exchange, routingKey, true, Encoding.UTF8.GetBytes("first"));
        await channel.BasicPublishAsync(exchange, routingKey, true, Encoding.UTF8.GetBytes("second"));

        // wait for both messages to be delivered
        allMessagesDelivered.Wait(timeout);

        // ack both messages at once by passing the last delivery tag
        await channel.BasicAckAsync(deliveryTags[1], true);

        // asserts queue is empty
        Assert.False(rabbitServer.Queues[queueName].HasMessages);
    }

    [Fact]
    public async Task AutoAck_is_honored_by_BasicConsume()
    {
        using var allMessagesDelivered = new ManualResetEventSlim();
        var deliveryTags = new List<ulong>();

        // setup a basic consumer that stores delivery tags to the deliveryTags list
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, e) =>
        {
            deliveryTags.Add(e.DeliveryTag);
            if (deliveryTags.Count == 2)
                allMessagesDelivered.Set();

            return Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(queueName, true, consumer);

        // publish two messages
        await channel.BasicPublishAsync(exchange, routingKey, true, Encoding.UTF8.GetBytes("first"));
        await channel.BasicPublishAsync(exchange, routingKey, true, Encoding.UTF8.GetBytes("second"));

        // wait for both messages to be delivered
        allMessagesDelivered.Wait(timeout * 1000000);

        // asserts queue is empty
        Assert.False(rabbitServer.Queues[queueName].HasMessages);
    }
}
