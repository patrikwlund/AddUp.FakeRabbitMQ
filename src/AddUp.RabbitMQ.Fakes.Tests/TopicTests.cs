using System.Diagnostics.CodeAnalysis;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace AddUp.RabbitMQ.Fakes
{
    [ExcludeFromCodeCoverage]
    public class TopicTests
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
        public void Publication_on_topic_is_consumed_with_wildcards(string bindingKey)
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
                consumerChannel.ExchangeDeclare(exchangeName, ExchangeType.Topic);
                consumerChannel.QueueBind(queueName, exchangeName, bindingKey, null);

                var consumer = new EventingBasicConsumer(consumerChannel);
                consumer.Received += (s, e) =>
                {
                    var message = Encoding.ASCII.GetString(e.Body);
                    var routingKey = e.RoutingKey;
                    var exchange = e.Exchange;

                    Assert.Equal("hello world!", message);
                    Assert.Equal("foo.bar.baz", routingKey);
                    Assert.Equal(exchangeName, exchange);

                    ok = true;
                };

                consumerChannel.BasicConsume(queueName, autoAck: true, consumer);

                // Publisher
                using (var publisherConnection = connectionFactory.CreateConnection())
                using (var publisherChannel = publisherConnection.CreateModel())
                {
                    const string message = "hello world!";
                    var messageBody = Encoding.ASCII.GetBytes(message);
                    publisherChannel.BasicPublish(exchangeName, "foo.bar.baz", false, null, messageBody);
                }
            }
            
            Assert.True(ok);
        }
    }
}
