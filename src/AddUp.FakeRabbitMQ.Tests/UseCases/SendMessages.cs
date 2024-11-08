using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using RabbitMQ.Client;
using Xunit;

namespace AddUp.RabbitMQ.Fakes.UseCases;

[ExcludeFromCodeCoverage]
public class SendMessages
{
    [Fact]
    public async Task SendToExchangeOnly()
    {
        var rabbitServer = new RabbitServer();
        var connectionFactory = new FakeConnectionFactory(rabbitServer);

        await using (var connection = await connectionFactory.CreateConnectionAsync())
        await using (var channel = await connection.CreateChannelAsync())
        {
            const string message = "hello world!";
            var messageBody = Encoding.ASCII.GetBytes(message);
            await channel.BasicPublishAsync(exchange: "my_exchange", routingKey: null, mandatory: false, body: messageBody);
        }

        Assert.Single(rabbitServer.Exchanges["my_exchange"].Messages);
    }

    [Fact]
    public async Task SendToExchangeWithBoundQueue()
    {
        var rabbitServer = new RabbitServer();
        var connectionFactory = new FakeConnectionFactory(rabbitServer);

        await ConfigureQueueBinding(rabbitServer, "my_exchange", "some_queue");

        using (var connection = await connectionFactory.CreateConnectionAsync())
        using (var channel = await connection.CreateChannelAsync())
        {
            const string message = "hello world!";
            var messageBody = Encoding.ASCII.GetBytes(message);
            await channel.BasicPublishAsync(exchange: "my_exchange", routingKey: null, mandatory: false, body: messageBody);
        }

        Assert.Equal(1, rabbitServer.Queues["some_queue"].MessageCount);
    }

    [Fact]
    public async Task SendToExchangeWithMultipleBoundQueues()
    {
        var rabbitServer = new RabbitServer();
        var connectionFactory = new FakeConnectionFactory(rabbitServer);

        await ConfigureQueueBinding(rabbitServer, "my_exchange", "some_queue");
        await ConfigureQueueBinding(rabbitServer, "my_exchange", "some_other_queue");

        using (var connection = await connectionFactory.CreateConnectionAsync())
        using (var channel = await connection.CreateChannelAsync())
        {
            const string message = "hello world!";
            var messageBody = Encoding.ASCII.GetBytes(message);
            await channel.BasicPublishAsync(exchange: "my_exchange", routingKey: null, mandatory: false, body: messageBody);
        }

        Assert.Equal(1, rabbitServer.Queues["some_queue"].MessageCount);
        Assert.Equal(1, rabbitServer.Queues["some_other_queue"].MessageCount);
    }

    [Fact]
    public async Task SendToExchangeWithAlternate()
    {
        var rabbitServer = new RabbitServer();
        var connectionFactory = new FakeConnectionFactory(rabbitServer);

        using (var connection = await connectionFactory.CreateConnectionAsync())
        using (var channel = await connection.CreateChannelAsync())
        {
            await channel.ExchangeDeclareAsync(exchange: "my_exchange", type: ExchangeType.Topic, arguments: new Dictionary<string, object> { ["alternate-exchange"] = "my_alternate_exchange" });
            await channel.ExchangeDeclareAsync(exchange: "my_alternate_exchange", type: ExchangeType.Fanout);
            await channel.QueueDeclareAsync(queue: "main_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
            await channel.QueueDeclareAsync(queue: "fallback_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

            await channel.QueueBindAsync("fallback_queue", "my_alternate_exchange", null);
            await channel.QueueBindAsync("main_queue", "my_exchange", "topic");
        }

        using (var connection = await connectionFactory.CreateConnectionAsync())
        using (var channel = await connection.CreateChannelAsync())
        {
            const string message = "hello world!";
            var messageBody = Encoding.ASCII.GetBytes(message);
            await channel.BasicPublishAsync(exchange: "my_exchange", routingKey: "other_topic", mandatory: false, body: messageBody);

            messageBody = Encoding.ASCII.GetBytes(message);
            await channel.BasicPublishAsync(exchange: "my_exchange", routingKey: "topic", mandatory: false, body: messageBody);
        }

        Assert.Equal(2, rabbitServer.Exchanges["my_exchange"].Messages.Count);
        Assert.Single(rabbitServer.Exchanges["my_alternate_exchange"].Messages);
        Assert.Equal(1, rabbitServer.Queues["fallback_queue"].MessageCount);
        Assert.Equal(1, rabbitServer.Queues["main_queue"].MessageCount);
    }

    private static async Task ConfigureQueueBinding(RabbitServer rabbitServer, string exchangeName, string queueName)
    {
        var connectionFactory = new FakeConnectionFactory(rabbitServer);
        using (var connection = await connectionFactory.CreateConnectionAsync())
        using (var channel = await connection.CreateChannelAsync())
        {
            await channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Direct);

            await channel.QueueBindAsync(queueName, exchangeName, null);
        }
    }
}