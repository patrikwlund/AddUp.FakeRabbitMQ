using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using Xunit;

namespace AddUp.RabbitMQ.Fakes
{
    [ExcludeFromCodeCoverage]
    public class FakeModelMiscTests
    {
        [Fact]
        public void CreateBasicProperties_returns_basic_properties()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                var result = model.CreateBasicProperties();
                Assert.NotNull(result);
            }
        }

        [Fact]
        public void MessageCount_is_zero_when_queue_is_just_created()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string queueName = "myQueue";
                model.QueueDeclare(queueName);
                Assert.Equal(0u, model.MessageCount(queueName));
            }
        }

        [Fact]
        public void MessageCount_returns_the_number_of_messages_in_the_queue()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string queueName = "myQueue";
                model.QueueDeclare(queueName);
                model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
                model.ExchangeBind(queueName, "my_exchange", null);

                var message = "hello world!";
                var encodedMessage = Encoding.ASCII.GetBytes(message);
                model.BasicPublish("my_exchange", null, new BasicProperties(), encodedMessage);

                Assert.Equal(1u, model.MessageCount(queueName));
            }
        }

        [Fact]
        public void MessageCount_returns_the_number_of_non_consumed_messages_in_the_queue()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string queueName = "myQueue";
                model.QueueDeclare(queueName);
                model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
                model.ExchangeBind(queueName, "my_exchange", null);

                for (var i = 0; i < 10; i++)
                {

                    var message = $"hello world: {i}";
                    var encodedMessage = Encoding.ASCII.GetBytes(message);
                    model.BasicPublish("my_exchange", null, new BasicProperties(), encodedMessage);
                }

                // Consume 4 messages
                var consumer = new EventingBasicConsumer(model);
                var consumptionCount = 0;
                consumer.Received += (s, e) =>
                {
                    if (consumptionCount >= 4) return;

                    model.BasicAck(e.DeliveryTag, false);
                    consumptionCount++;
                };

                model.BasicConsume(queueName, true, consumer);
                Assert.Equal(6u, model.MessageCount(queueName));
            }
        }

        [Fact]
        public void ConsumerCount_is_zero_when_queue_is_just_created()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string queueName = "myQueue";
                model.QueueDeclare(queueName);
                Assert.Equal(0u, model.ConsumerCount(queueName));
            }
        }

        [Fact]
        public void ConsumerCount_returns_the_number_of_attached_consumers()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string queueName = "myQueue";
                model.QueueDeclare(queueName);
                model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
                model.ExchangeBind(queueName, "my_exchange", null);

                // Attach 2 consumers
                model.BasicConsume(queueName, true, new DefaultBasicConsumer(model));
                model.BasicConsume(queueName, true, new DefaultBasicConsumer(model));

                Assert.Equal(2u, model.ConsumerCount(queueName));
            }
        }

        [Theory]
        [InlineData(true, 1)] // If requeue param to BasicNack is true, the message that is nacked should remain in Rabbit
        [InlineData(false, 0)] // If requeue param to BasicNack is false, the message that is nacked should be removed from Rabbit
        public void Nacking_message_does_not_reenqueue_a_brand_new_message(bool requeue, int expectedMessageCount)
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
                model.QueueDeclare("my_queue");
                model.ExchangeBind("my_queue", "my_exchange", null);

                var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
                model.BasicPublish("my_exchange", null, new BasicProperties(), encodedMessage);

                var consumer = new EventingBasicConsumer(model);
                model.BasicConsume("my_queue", false, consumer);
                Assert.True(consumer.IsRunning);

                var deliveryTag = model.WorkingMessagesForUnitTests.First().Key;
                model.BasicNack(deliveryTag, false, requeue);

                Assert.Equal(expectedMessageCount, server.Queues["my_queue"].Messages.Count);
                Assert.Equal(expectedMessageCount, model.WorkingMessagesForUnitTests.Count);
            }
        }
    }
}