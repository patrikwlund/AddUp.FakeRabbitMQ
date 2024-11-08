using System.Diagnostics.CodeAnalysis;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace AddUp.RabbitMQ.Fakes;

[ExcludeFromCodeCoverage]
public class ExchangeTopicTests
{
    [Theory]
    [InlineData("foo.bar.baz")]
    [InlineData("foo.bar.*")]
    [InlineData("foo.*.baz")]
    [InlineData("foo.*.*")]
    [InlineData("*.*.baz")]
    [InlineData("*.bar.*")]
    [InlineData("foo.#")]
    [InlineData("foo.bar.#")]
    [InlineData("*.bar.#")]
    [InlineData("#.baz")]
    [InlineData("#.bar.#")]
    [InlineData("#")]
    [InlineData("*.#")]
    [InlineData("#.*")]
    public async Task Publication_on_topic_is_consumed_with_wildcards(string bindingKey)
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
            await consumerChannel.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic);
            await consumerChannel.QueueBindAsync(queueName, exchangeName, bindingKey, null);

            var consumer = new AsyncEventingBasicConsumer(consumerChannel);
            using (var messageProcessed = new ManualResetEventSlim())
            {
                consumer.ReceivedAsync += (s, e) =>
                {
                    var message = Encoding.ASCII.GetString(e.Body.ToArray());
                    var routingKey = e.RoutingKey;
                    var exchange = e.Exchange;

                    Assert.Equal("hello world!", message);
                    Assert.Equal("foo.bar.baz", routingKey);
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
                    await publisherChannel.BasicPublishAsync(exchangeName, "foo.bar.baz", false, messageBody);
                }

                messageProcessed.Wait();
            }
        }

        Assert.True(ok);

        var exchange = rabbitServer.Exchanges[exchangeName];
        Assert.Empty(exchange.DroppedMessages);
    }
}
