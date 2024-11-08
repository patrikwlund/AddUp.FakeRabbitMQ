using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using FluentAssertions;
using RabbitMQ.Client;
using Xunit;

namespace AddUp.RabbitMQ.Fakes.UseCases;

[ExcludeFromCodeCoverage]
public class ReceiveMessages
{
    [Fact]
    public async Task ReceiveMessagesOnQueue()
    {
        var rabbitServer = new RabbitServer();

        await ConfigureQueueBinding(rabbitServer, "my_exchange", "my_queue");
        await SendMessage(rabbitServer,"my_exchange","hello_world");

        var connectionFactory = new FakeConnectionFactory(rabbitServer);
        await using (var connection = await connectionFactory.CreateConnectionAsync())
        await using (var channel = await connection.CreateChannelAsync())
        {
            // First message
            var message = await channel.BasicGetAsync("my_queue", autoAck: false);
            
            Assert.NotNull(message);
            var messageBody = Encoding.ASCII.GetString(message.Body.ToArray());

            Assert.Equal("hello_world", messageBody);

            await channel.BasicAckAsync(message.DeliveryTag,multiple:false);
        }
    }

    [Fact]
    public async Task ReceiveMessagesOnQueueWithBasicProperties()
    {
        var rabbitServer = new RabbitServer();

        await ConfigureQueueBinding(rabbitServer, "my_exchange", "my_queue");
        var basicProperties = new BasicProperties
        {
            Headers = new Dictionary<string, object>() { { "TestKey", "TestValue" } },
            CorrelationId = Guid.NewGuid().ToString(),
            ReplyTo = "TestQueue",
            Timestamp = new AmqpTimestamp(123456),
            ReplyToAddress = new PublicationAddress("exchangeType", "excahngeName", "routingKey"),
            ClusterId = "1",
            ContentEncoding = "encoding",
            ContentType = "type",
            DeliveryMode = DeliveryModes.Transient,
            Expiration = "none",
            MessageId = "id",
            Priority = 1,
            Type = "type",
            UserId = "1",
            AppId = "1"
        };

        await SendMessage(rabbitServer, "my_exchange", "hello_world", basicProperties);

        var connectionFactory = new FakeConnectionFactory(rabbitServer);
        await using (var connection = await connectionFactory.CreateConnectionAsync())
        await using (var channel = await connection.CreateChannelAsync())
        {
            // First message
            var message = await channel.BasicGetAsync("my_queue", autoAck: false);

            Assert.NotNull(message);
            var messageBody = Encoding.ASCII.GetString(message.Body.ToArray());

            Assert.Equal("hello_world", messageBody);

            var actualBasicProperties = message.BasicProperties;
            actualBasicProperties.Should().BeEquivalentTo(basicProperties);

            await channel.BasicAckAsync(message.DeliveryTag, multiple: false);
        }
    }

    [Fact]
    public async Task QueueingConsumer_MessagesOnQueueBeforeConsumerIsCreated_ReceiveMessagesOnQueue()
    {
        var rabbitServer = new RabbitServer();

        await ConfigureQueueBinding(rabbitServer, "my_exchange", "my_queue");
        await SendMessage(rabbitServer, "my_exchange", "hello_world");

        var connectionFactory = new FakeConnectionFactory(rabbitServer);
        await using var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        var consumer = new QueueingBasicConsumer(channel);
        await channel.BasicConsumeAsync("my_queue", false, consumer);

        if (consumer.Queue.Dequeue(5000, out var messageOut))
        {
            var messageBody = Encoding.ASCII.GetString(messageOut.Body.ToArray());
            Assert.Equal("hello_world", messageBody);
            await channel.BasicAckAsync(messageOut.DeliveryTag, multiple: false);
        }

        Assert.NotNull(messageOut);
    }

    [Fact]
    public async Task QueueingConsumer_MessagesSentAfterConsumerIsCreated_ReceiveMessagesOnQueue()
    {
        var rabbitServer = new RabbitServer();

        await ConfigureQueueBinding(rabbitServer, "my_exchange", "my_queue");
       
        var connectionFactory = new FakeConnectionFactory(rabbitServer);
        await using var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var consumer = new QueueingBasicConsumer(channel);
        await channel.BasicConsumeAsync("my_queue", false, consumer);

        await SendMessage(rabbitServer, "my_exchange", "hello_world");

        if (consumer.Queue.Dequeue(5000, out var messageOut))
        {
            var messageBody = Encoding.ASCII.GetString(messageOut.Body.ToArray());
            Assert.Equal("hello_world", messageBody);
            await channel.BasicAckAsync(messageOut.DeliveryTag, multiple: false);
        }

        Assert.NotNull(messageOut);
    }

    private static async Task SendMessage(RabbitServer rabbitServer, string exchange, string message, BasicProperties basicProperties = null)
    {
        var connectionFactory = new FakeConnectionFactory(rabbitServer);

        using var connection = await connectionFactory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        var messageBody = Encoding.ASCII.GetBytes(message);
        await channel.BasicPublishAsync(exchange: exchange, routingKey: null, mandatory: false, basicProperties: basicProperties, body: messageBody);
    }

    private static async Task ConfigureQueueBinding(RabbitServer rabbitServer, string exchangeName, string queueName)
    {
        var connectionFactory = new FakeConnectionFactory(rabbitServer);
        await using var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Direct);

        await channel.QueueBindAsync(queueName, exchangeName, null);
    }
}