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
    public class FakeModelBasicTests
    {
        [Fact]
        public void BasicAck_removes_message_from_queue()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
                model.QueueDeclare("my_queue");
                model.ExchangeBind("my_queue", "my_exchange", null);

                var message = "hello world!";
                var encodedMessage = Encoding.ASCII.GetBytes(message);
                model.BasicPublish("my_exchange", null, new BasicProperties(), encodedMessage);

                var consumer = new EventingBasicConsumer(model);
                model.BasicConsume("my_queue", false, consumer);
                Assert.True(consumer.IsRunning);

                var deliveryTag = model.WorkingMessagesForUnitTests.First().Key;
                model.BasicAck(deliveryTag, false);

                Assert.Empty(server.Queues["my_queue"].Messages);
            }
        }

        [Fact]
        public void BasicCancel_removes_a_consumer()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.QueueDeclare("my_queue");
                var expectedConsumerTag = "foo";
                var actualConsumerTag = "";

                var consumer = new EventingBasicConsumer(model) { ConsumerTag = expectedConsumerTag };
                consumer.Unregistered += (s, e) => actualConsumerTag = e.ConsumerTag;

                model.BasicConsume("my_queue", false, expectedConsumerTag, consumer);
                Assert.True(consumer.IsRunning);
                model.BasicCancel(expectedConsumerTag);
                Assert.False(consumer.IsRunning);

                Assert.Equal(expectedConsumerTag, actualConsumerTag);
            }
        }

        [Fact]
        public void BasicCancel_works_for_asynchronous_consumers()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.QueueDeclare("my_queue");
                var expectedConsumerTag = "foo";

                var consumer = new FakeAsyncDefaultBasicConsumer(model);

                model.BasicConsume("my_queue", false, expectedConsumerTag, consumer);
                Assert.True(consumer.IsRunning);
                model.BasicCancel(expectedConsumerTag);
                Assert.False(consumer.IsRunning);

                Assert.Equal(expectedConsumerTag, consumer.LastCancelOkConsumerTag);
            }
        }

        [Fact]
        public void BasicConsume_works_for_asynchronous_consumers()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
                model.QueueDeclare("my_queue");
                model.ExchangeBind("my_queue", "my_exchange", null);

                var message = "hello world!";
                var encodedMessage = Encoding.ASCII.GetBytes(message);
                model.BasicPublish("my_exchange", null, new BasicProperties(), encodedMessage);

                var consumer = new FakeAsyncDefaultBasicConsumer(model);
                model.BasicConsume("my_queue", false, consumer);
                Assert.True(consumer.IsRunning);

                Assert.Equal(encodedMessage, consumer.LastDelivery.body);
            }
        }

        [Fact]
        public void BasicGet_retrieves_message_from_the_queue()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
                model.QueueDeclare("my_queue");
                model.ExchangeBind("my_queue", "my_exchange", null);

                var message = "hello world!";
                var encodedMessage = Encoding.ASCII.GetBytes(message);
                model.BasicPublish("my_exchange", null, new BasicProperties(), encodedMessage);

                var response = model.BasicGet("my_queue", false);

                Assert.Equal(encodedMessage, response.Body);
                Assert.True(response.DeliveryTag > 0ul);
            }
        }

        [Fact]
        public void BasicGet_with_no_messages_returns_null()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.QueueDeclare("my_queue");
                var response = model.BasicGet("my_queue", false);
                Assert.Null(response);
            }
        }

        [Fact]
        public void BasicGet_with_no_queue_returns_null()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                var response = model.BasicGet("my_queue", false);
                Assert.Null(response);
            }
        }

        [Theory]
        [InlineData(true, 0)] // BasicGet WITH auto-ack SHOULD remove the message from the queue
        [InlineData(false, 1)] // BasicGet with NO auto-ack should NOT remove the message from the queue
        public void BasicGet_does_not_remove_the_message_from_the_queue_if_not_acked(bool autoAck, int expectedMessageCount)
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
                model.QueueDeclare("my_queue");
                model.ExchangeBind("my_queue", "my_exchange", null);

                var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
                model.BasicPublish("my_exchange", null, new BasicProperties(), encodedMessage);

                _ = model.BasicGet("my_queue", autoAck);

                Assert.Equal(expectedMessageCount, server.Queues["my_queue"].Messages.Count);
                Assert.Equal(expectedMessageCount, model.WorkingMessagesForUnitTests.Count);
            }
        }

        [Fact]
        public void BasicPublish_publishes_message()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
                model.QueueDeclare("my_queue");
                model.ExchangeBind("my_queue", "my_exchange", null);

                var message = "hello world!";
                var encodedMessage = Encoding.ASCII.GetBytes(message);

                model.BasicPublish("my_exchange", null, new BasicProperties(), encodedMessage);

                Assert.Single(server.Queues["my_queue"].Messages);
                Assert.Equal(encodedMessage, server.Queues["my_queue"].Messages.First().Body);
            }
        }

        [Fact]
        public void BasicPublishBatch_publishes_messages()
        {
            var node = new RabbitServer();
            using (var model = new FakeModel(node))
            {
                model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
                model.QueueDeclare("my_queue");
                model.ExchangeBind("my_queue", "my_exchange", null);

                var messages = new[] { "hello world!", "Thank you, @inbarbarkai" };
                var encodedMessages = messages.Select(m => Encoding.ASCII.GetBytes(m)).ToArray();

                var batch = model.CreateBasicPublishBatch();
                batch.Add("my_exchange", null, true, new BasicProperties(), encodedMessages[0]);
                batch.Add("my_exchange", null, true, new BasicProperties(), encodedMessages[1]);
                batch.Publish();

                Assert.Equal(2, node.Queues["my_queue"].Messages.Count);

                var index = 0;
                foreach (var item in node.Queues["my_queue"].Messages)
                {
                    Assert.Equal(encodedMessages[index], item.Body);
                    index++;
                }
            }
        }
    }
}
