using System.Diagnostics.CodeAnalysis;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace AddUp.RabbitMQ.Fakes;

[ExcludeFromCodeCoverage]
public class ExchangeHeadersTests
{
    [Theory]
    [InlineData("routing-key", true)]
    [InlineData("routing-key-2", true)]
    [InlineData("", true)]
    public async Task Publication_on_headers_is_always_consumed_because_it_is_not_implemented_yet(string routingKey, bool shouldBeOK)
    {
        const string exchangeName = "my_exchange";
        const string queueName = "my_queue";

        var rabbitServer = new RabbitServer();
        var connectionFactory = new FakeConnectionFactory(rabbitServer);

        var ok = false;

        // Consumer
        await using (var consumerConnection = await connectionFactory.CreateConnectionAsync())
        await using (var consumerChannel = await consumerConnection.CreateChannelAsync())
        {
            await consumerChannel.QueueDeclareAsync(queueName, false, false, false, null);
            await consumerChannel.ExchangeDeclareAsync(exchangeName, ExchangeType.Headers);
            await consumerChannel.QueueBindAsync(queueName, exchangeName, "whatever", null);

            var consumer = new AsyncEventingBasicConsumer(consumerChannel);
            using (var messageProcessed = new ManualResetEventSlim(!shouldBeOK))
            {
                consumer.ReceivedAsync += (s, e) =>
                {
                    var message = Encoding.ASCII.GetString(e.Body.ToArray());
                    var exchange = e.Exchange;

                    Assert.Equal("hello world!", message);
                    Assert.Equal(exchangeName, exchange);

                    ok = true;
                    messageProcessed.Set();

                    return Task.CompletedTask;
                };

                await consumerChannel.BasicConsumeAsync(queueName, autoAck: true, consumer);

                // Publisher
                await using (var publisherConnection = await connectionFactory.CreateConnectionAsync())
                await using (var publisherChannel = await publisherConnection.CreateChannelAsync())
                {
                    const string message = "hello world!";
                    var messageBody = Encoding.ASCII.GetBytes(message);
                    await publisherChannel.BasicPublishAsync(exchangeName, routingKey, false, messageBody);
                }

                messageProcessed.Wait();
            }
        }

        Assert.Equal(ok, shouldBeOK);

        var exchange = rabbitServer.Exchanges[exchangeName];
        var expectedDroppedMessages = shouldBeOK ? 0 : 1;
        Assert.Equal(expectedDroppedMessages, exchange.DroppedMessages.Count);
    }
}
