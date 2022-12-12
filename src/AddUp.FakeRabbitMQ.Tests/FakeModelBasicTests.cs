using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using FluentAssertions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
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
                model.QueueDeclare("my_queue1");
                model.QueueDeclare("my_queue2");
                var expectedConsumerTag = "foo";
                var actualConsumerTag = "";

                var consumer1 = new EventingBasicConsumer(model);
                var receivedHasRan = false;
                consumer1.Received += (s, e) => receivedHasRan = true;
                consumer1.Unregistered += (s, e) => actualConsumerTag = e.ConsumerTags.First();

                model.BasicConsume("my_queue1", false, expectedConsumerTag, consumer1);
                Assert.True(consumer1.IsRunning);
                model.BasicCancel(expectedConsumerTag);
                Assert.False(consumer1.IsRunning);

                model.BasicPublish("", "my_queue1", model.CreateBasicProperties(), Encoding.ASCII.GetBytes("hello"));

                using (var allowedThrough = new ManualResetEventSlim())
                {
                    // We're relying on the fact that we deliver messages sequentially and in order here.
                    // We can check that the message for consumer1 would have been processed by waiting
                    // for the message for consumer2 - either message 1 was delivered first (in which case we
                    // fail the test), or message 1 was not delivered at all, which is what we want.
                    var consumer2 = new EventingBasicConsumer(model);
                    consumer2.Received += (s, e) => allowedThrough.Set();
                    model.BasicPublish("", "my_queue2", model.CreateBasicProperties(), Encoding.ASCII.GetBytes("bonjour"));
                    model.BasicConsume("my_queue2", true, consumer2);

                    var wasAllowedThrough = allowedThrough.Wait(10000);
                    Assert.True(wasAllowedThrough);
                }

                Assert.Equal(expectedConsumerTag, actualConsumerTag);
                Assert.False(receivedHasRan);
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
        public void BasicConsume_generates_consumer_tag_if_none_specified()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
                model.QueueDeclare("my_queue");
                model.ExchangeBind("my_queue", "my_exchange", null);

                var consumer = new FakeAsyncDefaultBasicConsumer(model);
                var consumerTag = model.BasicConsume("my_queue", false, "", false, false, null, consumer);

                Assert.NotNull(consumerTag);
                Assert.NotEmpty(consumerTag);
            }
        }

        [Fact]
        public void BasicConsume_with_duplicate_consumer_tag_fails()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
                model.QueueDeclare("my_queue");
                model.ExchangeBind("my_queue", "my_exchange", null);

                var consumerTag = "foo";
                var consumer = new FakeAsyncDefaultBasicConsumer(model);
                model.BasicConsume("my_queue", false, consumerTag, consumer);
                Assert.Throws<OperationInterruptedException>(() =>
                    model.BasicConsume("my_queue", false, consumerTag, consumer));
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
        public void BasicPublish_after_BasicConsume_with_BlockingDelivery_is_synchronous()
        {
            var server = new RabbitServer { BlockingConsumerDelivery = true };
            using var model = new FakeModel(server);

            model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            model.QueueDeclare("my_queue");
            model.ExchangeBind("my_queue", "my_exchange", null);

            var trackedValue = new AsyncLocal<string>
            {
                Value = "initial"
            };

            var consumer = new EventingBasicConsumer(model);
            consumer.Received += (_, _) =>
            {
                trackedValue.Value = "from_consumer";
            };

            model.BasicConsume("my_queue", false, consumer);

            var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
            model.BasicPublish("my_exchange", null, model.CreateBasicProperties(), encodedMessage);

            trackedValue.Value.Should().Be("from_consumer");
        }

        [Fact]
        public void BasicPublish_before_BasicConsume_with_BlockingDelivery_is_synchronous()
        {
            var server = new RabbitServer { BlockingConsumerDelivery = true };
            using var model = new FakeModel(server);

            model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            model.QueueDeclare("my_queue");
            model.ExchangeBind("my_queue", "my_exchange", null);

            var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
            model.BasicPublish("my_exchange", null, model.CreateBasicProperties(), encodedMessage);

            var trackedValue = new AsyncLocal<string>
            {
                Value = "initial"
            };

            var consumer = new EventingBasicConsumer(model);
            consumer.Received += (_, _) =>
            {
                trackedValue.Value = "from_consumer";
            };

            model.BasicConsume("my_queue", false, consumer);

            trackedValue.Value.Should().Be("from_consumer");
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
