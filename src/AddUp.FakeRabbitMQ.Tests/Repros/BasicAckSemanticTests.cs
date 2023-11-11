using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace AddUp.RabbitMQ.Fakes.Repros;

// Ensures issues described in https://github.com/addupsolutions/AddUp.FakeRabbitMQ/issues/180 are fixed
[ExcludeFromCodeCoverage]
public sealed class BasicAckSemanticTests : IDisposable
{
    private const string exchange = "exchange";
    private const string queueName = "queue";
    private const string routingKey = "routing-key";
    private const int timeout = 1000;
    private readonly RabbitServer rabbitServer;
    private readonly IConnection connection;
    private readonly IModel model;

    // Setup
    public BasicAckSemanticTests()
    {
        rabbitServer = new RabbitServer();
        connection = new FakeConnectionFactory(rabbitServer).CreateConnection();
        model = connection.CreateModel();

        model.ExchangeDeclare(exchange, ExchangeType.Topic);
        model.QueueDeclare(queueName);
        model.QueueBind(queueName, exchange, "#");
    }

    // Teardown
    public void Dispose()
    {
        model.Dispose();
        connection.Dispose();
    }

    [Fact]
    public void Ack_DeliveryTagsAreRespected()
    {
        using var allMessagesDelivered = new ManualResetEventSlim();
        var deliveryTags = new List<ulong>();

        // setup a basic consumer that stores delivery tags to the deliveryTags list
        var consumer = new EventingBasicConsumer(model);
        consumer.Received += (_, args) =>
        {
            deliveryTags.Add(args.DeliveryTag);
            if (deliveryTags.Count == 2)
                allMessagesDelivered.Set();
        };

        model.BasicConsume(queueName, false, consumer);

        // publish two messages
        model.BasicPublish(exchange, routingKey, true, null, Encoding.UTF8.GetBytes("first"));
        model.BasicPublish(exchange, routingKey, true, null, Encoding.UTF8.GetBytes("second"));

        // wait for both messages to be delivered
        allMessagesDelivered.Wait(timeout);

        // ack the **second** message, but not the first one
        model.BasicAck(deliveryTags[1], false);

        // asserts that only one message is still queued
        Assert.True(rabbitServer.Queues[queueName].Messages.Count == 1, "Only one message is still queued");

        // asserts that the remaining message in queue is the first one, since we acked the second
        rabbitServer.Queues[queueName].Messages.TryPeek(out var pendingMessage);
        Assert.True(Encoding.UTF8.GetString(pendingMessage.Body) == "first", "The remaining message in queue is the first one");
    }

    [Fact]
    public void Ack_Multiple()
    {
        using var allMessagesDelivered = new ManualResetEventSlim();
        var deliveryTags = new List<ulong>();

        // setup a basic consumer that stores delivery tags to the deliveryTags list
        var consumer = new EventingBasicConsumer(model);
        consumer.Received += (_, args) =>
        {
            deliveryTags.Add(args.DeliveryTag);
            if (deliveryTags.Count == 2)
                allMessagesDelivered.Set();
        };

        model.BasicConsume(queueName, false, consumer);

        // publish two messages
        model.BasicPublish(exchange, routingKey, true, null, Encoding.UTF8.GetBytes("first"));
        model.BasicPublish(exchange, routingKey, true, null, Encoding.UTF8.GetBytes("second"));

        // wait for both messages to be delivered
        allMessagesDelivered.Wait(timeout);

        // ack both messages at once by passing the last delivery tag
        model.BasicAck(deliveryTags[1], true);

        // asserts queue is empty
        Assert.Empty(rabbitServer.Queues[queueName].Messages);
    }
}
