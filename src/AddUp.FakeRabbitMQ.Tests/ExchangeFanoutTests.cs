using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace AddUp.RabbitMQ.Fakes;

[ExcludeFromCodeCoverage]
public class ExchangeFanoutTests
{
    [Theory]
    [InlineData("routing-key", true)]
    [InlineData("routing-key-2", true)]
    [InlineData("", true)]
    public void Publication_on_fanout_is_always_consumed(string routingKey, bool shouldBeOK)
    {
        const string exchangeName = "my_exchange";
        const string queueName = "my_queue";

        var rabbitServer = new RabbitServer();
        var connectionFactory = new FakeConnectionFactory(rabbitServer);

        var ok = false;

        // Consumer
        using (var consumerConnection = connectionFactory.CreateConnection())
        using (var consumerChannel = consumerConnection.CreateModel())
        {
            consumerChannel.QueueDeclare(queueName, false, false, false, null);
            consumerChannel.ExchangeDeclare(exchangeName, ExchangeType.Fanout);
            consumerChannel.QueueBind(queueName, exchangeName, "whatever", null);

            var consumer = new EventingBasicConsumer(consumerChannel);
            using (var messageProcessed = new ManualResetEventSlim(!shouldBeOK))
            {
                consumer.Received += (s, e) =>
                {
                    var message = Encoding.ASCII.GetString(e.Body.ToArray());
                    var exchange = e.Exchange;

                    Assert.Equal("hello world!", message);
                    Assert.Equal(exchangeName, exchange);

                    ok = true;
                    messageProcessed.Set();
                };

                consumerChannel.BasicConsume(queueName, autoAck: true, consumer);

                // Publisher
                using (var publisherConnection = connectionFactory.CreateConnection())
                using (var publisherChannel = publisherConnection.CreateModel())
                {
                    const string message = "hello world!";
                    var messageBody = Encoding.ASCII.GetBytes(message);
                    publisherChannel.BasicPublish(exchangeName, routingKey, false, null, messageBody);
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
