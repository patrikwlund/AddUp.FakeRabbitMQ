using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
                model.BasicPublish("my_exchange", null, model.CreateBasicProperties(), encodedMessage);

                var consumer = new EventingBasicConsumer(model);

                using (var messageProcessed = new ManualResetEventSlim())
                {
                    consumer.Received += (_, _) => messageProcessed.Set();
                    model.BasicConsume("my_queue", false, consumer);
                    Assert.True(consumer.IsRunning);

                    messageProcessed.Wait();
                    var deliveryTag = model.WorkingMessagesForUnitTests.First().Key;
                    model.BasicAck(deliveryTag, false);

                    Assert.Empty(server.Queues["my_queue"].Messages);
                }
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

                var consumer = new EventingBasicConsumer(model);
                consumer.Unregistered += (s, e) => actualConsumerTag = e.ConsumerTags.First();

                model.BasicConsume("my_queue", false, expectedConsumerTag, consumer);
                Assert.True(consumer.IsRunning);
                model.BasicCancel(expectedConsumerTag);
                Assert.False(consumer.IsRunning);

                Assert.Equal(expectedConsumerTag, actualConsumerTag);
            }
        }

        [Fact]
        public void BasicCancelNoWait_removes_a_consumer()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.QueueDeclare("my_queue");
                var expectedConsumerTag = "foo";
                var actualConsumerTag = "";

                var consumer = new EventingBasicConsumer(model);
                consumer.Unregistered += (s, e) => actualConsumerTag = e.ConsumerTags.First();

                model.BasicConsume("my_queue", false, expectedConsumerTag, consumer);
                Assert.True(consumer.IsRunning);
                model.BasicCancelNoWait(expectedConsumerTag);
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
                consumer.LastCancelOkConsumerTag.Task.Wait();
                Assert.False(consumer.IsRunning);

                Assert.Equal(expectedConsumerTag, consumer.LastCancelOkConsumerTag.Task.Result);
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
                model.BasicPublish("my_exchange", null, model.CreateBasicProperties(), encodedMessage);

                var consumer = new FakeAsyncDefaultBasicConsumer(model);
                model.BasicConsume("my_queue", false, consumer);
                Assert.True(consumer.IsRunning);
                consumer.LastDelivery.Task.Wait();

                Assert.Equal(encodedMessage, consumer.LastDelivery.Task.Result.body);
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
                model.BasicPublish("my_exchange", null, model.CreateBasicProperties(), encodedMessage);

                var response = model.BasicGet("my_queue", false);

                Assert.Equal(encodedMessage, response.Body.ToArray());
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
                model.BasicPublish("my_exchange", null, model.CreateBasicProperties(), encodedMessage);

                _ = model.BasicGet("my_queue", autoAck);

                Assert.Equal(expectedMessageCount, server.Queues["my_queue"].Messages.Count);
                Assert.Equal(expectedMessageCount, model.WorkingMessagesForUnitTests.Count);
            }
        }

        [Theory]
        [InlineData(true, 1)] // If requeue param to BasicNack is true, the message that is nacked should remain in Rabbit
        [InlineData(false, 0)] // If requeue param to BasicNack is false, the message that is nacked should be removed from Rabbit
        public void BasicNack_does_not_reenqueue_a_brand_new_message(bool requeue, int expectedMessageCount)
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
                model.QueueDeclare("my_queue");
                model.ExchangeBind("my_queue", "my_exchange", null);

                var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
                model.BasicPublish("my_exchange", null, model.CreateBasicProperties(), encodedMessage);

                var consumer = new EventingBasicConsumer(model);
                using (var messageProcessed = new ManualResetEventSlim())
                {
                    consumer.Received += (_, _) => messageProcessed.Set();
                    model.BasicConsume("my_queue", false, consumer);
                    Assert.True(consumer.IsRunning);

                    messageProcessed.Wait();
                    var deliveryTag = model.WorkingMessagesForUnitTests.First().Key;
                    model.BasicNack(deliveryTag, false, requeue);

                    Assert.Equal(expectedMessageCount, server.Queues["my_queue"].Messages.Count);
                    Assert.Equal(expectedMessageCount, model.WorkingMessagesForUnitTests.Count);
                }
            }
        }

        [Theory]
        [InlineData(true, 1)] // If requeue param to BasicNack is true, the message that is nacked should remain in Rabbit
        [InlineData(false, 0)] // If requeue param to BasicNack is false, the message that is nacked should be removed from Rabbit
        public void BasicReject_does_not_reenqueue_a_brand_new_message(bool requeue, int expectedMessageCount)
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
                model.QueueDeclare("my_queue");
                model.ExchangeBind("my_queue", "my_exchange", null);

                var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
                model.BasicPublish("my_exchange", null, model.CreateBasicProperties(), encodedMessage);

                var consumer = new EventingBasicConsumer(model);
                using (var messageProcessed = new ManualResetEventSlim())
                {
                    consumer.Received += (_, _) => messageProcessed.Set();
                    model.BasicConsume("my_queue", false, consumer);
                    Assert.True(consumer.IsRunning);

                    messageProcessed.Wait();
                    var deliveryTag = model.WorkingMessagesForUnitTests.First().Key;
                    model.BasicReject(deliveryTag, requeue);

                    Assert.Equal(expectedMessageCount, server.Queues["my_queue"].Messages.Count);
                    Assert.Equal(expectedMessageCount, model.WorkingMessagesForUnitTests.Count);
                }
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

                model.BasicPublish("my_exchange", null, model.CreateBasicProperties(), encodedMessage);

                Assert.Single(server.Queues["my_queue"].Messages);
                Assert.Equal(encodedMessage, server.Queues["my_queue"].Messages.First().Body);
            }
        }

        [Fact]
        public void BasicPublish_to_default_exchange_publishes_message()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.QueueDeclare("my_queue");

                var message = "hello world!";
                var encodedMessage = Encoding.ASCII.GetBytes(message);

                model.BasicPublish("", "my_queue", model.CreateBasicProperties(), encodedMessage);

                Assert.Single(server.Queues["my_queue"].Messages);
                Assert.Equal(encodedMessage, server.Queues["my_queue"].Messages.First().Body);
            }
        }

        [Fact]
        public void BasicPublish_before_BasicConsume_does_not_deadlock()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
                model.QueueDeclare("my_queue");
                model.ExchangeBind("my_queue", "my_exchange", null);

                var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
                model.BasicPublish("my_exchange", null, model.CreateBasicProperties(), encodedMessage);

                using (var deadlockDetector = new ManualResetEventSlim())
                using (var allowedThrough = new ManualResetEventSlim())
                {
                    var consumer = new EventingBasicConsumer(model);
                    consumer.Received += (_, _) =>
                    {
                        if (deadlockDetector.Wait(10000))
                        {
                            allowedThrough.Set();
                        }
                    };

                    model.BasicConsume("my_queue", false, consumer);

                    deadlockDetector.Set();
                    var wasAllowedThrough = allowedThrough.Wait(10000);
                    Assert.True(wasAllowedThrough);
                }
            }
        }

        [Fact]
        public void BasicPublish_after_BasicConsume_does_not_deadlock()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
                model.QueueDeclare("my_queue");
                model.ExchangeBind("my_queue", "my_exchange", null);

                using (var deadlockDetector = new ManualResetEventSlim())
                using (var allowedThrough = new ManualResetEventSlim())
                {
                    var consumer = new EventingBasicConsumer(model);
                    consumer.Received += (_, _) =>
                    {
                        if (deadlockDetector.Wait(10000))
                        {
                            allowedThrough.Set();
                        }
                    };

                    model.BasicConsume("my_queue", false, consumer);

                    var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
                    model.BasicPublish("my_exchange", null, model.CreateBasicProperties(), encodedMessage);

                    deadlockDetector.Set();
                    var wasAllowedThrough = allowedThrough.Wait(10000);
                    Assert.True(wasAllowedThrough);
                }
            }
        }

        [Fact]
        public void BasicPublishBatch_publishes_messages()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
                model.QueueDeclare("my_queue");
                model.ExchangeBind("my_queue", "my_exchange", null);

                var messages = new[] { "hello world!", "Thank you, @inbarbarkai" };
                var encodedMessages = messages
                    .Select(m => new ReadOnlyMemory<byte>(Encoding.ASCII.GetBytes(m)))
                    .ToArray();

                var batch = model.CreateBasicPublishBatch();
                batch.Add("my_exchange", null, true, model.CreateBasicProperties(), encodedMessages[0]);
                batch.Add("my_exchange", null, true, model.CreateBasicProperties(), encodedMessages[1]);
                batch.Publish();

                Assert.Equal(2, server.Queues["my_queue"].Messages.Count);

                var index = 0;
                foreach (var item in server.Queues["my_queue"].Messages)
                {
                    Assert.Equal(encodedMessages[index].ToArray(), item.Body);
                    index++;
                }
            }
        }

        [Fact]
        public void BasicQos_does_nothing_because_it_is_not_implemented_yet()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
                model.BasicQos(1u, 1, true);
            Assert.True(true);
        }
    }
}
