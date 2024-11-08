using System.Diagnostics.CodeAnalysis;
using System.Text;
using FluentAssertions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace AddUp.RabbitMQ.Fakes;

[ExcludeFromCodeCoverage]
public class FakeModelBasicTests
{
    [Fact]
    public async Task BasicAck_removes_message_from_queue()
    {
        var server = new RabbitServer();
        await using (var model = new FakeModel(server))
        {
            await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
            await model.QueueDeclareAsync("my_queue");
            await model.ExchangeBindAsync("my_queue", "my_exchange", null);

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);
            await model.BasicPublishAsync("my_exchange", null, encodedMessage);

            var consumer = new AsyncEventingBasicConsumer(model);

            using (var messageProcessed = new ManualResetEventSlim())
            {
                consumer.ReceivedAsync += (_, _) => { messageProcessed.Set(); return Task.CompletedTask; };

                await model.BasicConsumeAsync("my_queue", false, consumer);
                Assert.True(consumer.IsRunning);

                messageProcessed.Wait();
                var deliveryTag = model.WorkingMessagesForUnitTests.First().Key;
                await model.BasicAckAsync(deliveryTag, false);

                Assert.False(server.Queues["my_queue"].HasMessages);
            }
        }
    }

    [Fact]
    public async Task BasicCancel_removes_a_consumer()
    {
        var server = new RabbitServer();
        await using (var model = new FakeModel(server))
        {
            await model.QueueDeclareAsync("my_queue1");
            await model.QueueDeclareAsync("my_queue2");
            var expectedConsumerTag = "foo";
            var actualConsumerTag = "";

            var consumer1 = new AsyncEventingBasicConsumer(model);
            var receivedHasRan = false;
            consumer1.ReceivedAsync += (s, e) => { receivedHasRan = true; return Task.CompletedTask; };
            consumer1.UnregisteredAsync += (s, e) =>
            {
                actualConsumerTag = e.ConsumerTags[0];
                return Task.CompletedTask;
            };

            await model.BasicConsumeAsync("my_queue1", false, expectedConsumerTag, consumer1);
            Assert.True(consumer1.IsRunning);
            await model.BasicCancelAsync(expectedConsumerTag);
            Assert.False(consumer1.IsRunning);

            await model.BasicPublishAsync("", "my_queue1", Encoding.ASCII.GetBytes("hello"));

            using (var allowedThrough = new ManualResetEventSlim())
            {
                // We're relying on the fact that we deliver messages sequentially and in order here.
                // We can check that the message for consumer1 would have been processed by waiting
                // for the message for consumer2 - either message 1 was delivered first (in which case we
                // fail the test), or message 1 was not delivered at all, which is what we want.
                var consumer2 = new AsyncEventingBasicConsumer(model);
                consumer2.ReceivedAsync += (s, e) =>
                {
                    allowedThrough.Set();
                    return Task.CompletedTask;
                };

                await model.BasicPublishAsync("", "my_queue2", Encoding.ASCII.GetBytes("bonjour"));
                await model.BasicConsumeAsync("my_queue2", true, consumer2);

                var wasAllowedThrough = allowedThrough.Wait(10000);
                Assert.True(wasAllowedThrough);
            }

            Assert.Equal(expectedConsumerTag, actualConsumerTag);
            Assert.False(receivedHasRan);
        }
    }

    [Fact]
    public async Task BasicCancelNoWait_removes_a_consumer()
    {
        var server = new RabbitServer();
        await using (var model = new FakeModel(server))
        {
            await model.QueueDeclareAsync("my_queue");
            var expectedConsumerTag = "foo";
            var actualConsumerTag = "";

            var consumer = new AsyncEventingBasicConsumer(model);
            consumer.UnregisteredAsync += (s, e) =>
            {
                actualConsumerTag = e.ConsumerTags[0];
                return Task.CompletedTask;
            };

            await model.BasicConsumeAsync("my_queue", false, expectedConsumerTag, consumer);
            Assert.True(consumer.IsRunning);
            await model.BasicCancelAsync(expectedConsumerTag, noWait: true);
            Assert.False(consumer.IsRunning);

            Assert.Equal(expectedConsumerTag, actualConsumerTag);
        }
    }

    [Fact]
    public async Task BasicCancel_works_for_asynchronous_consumers()
    {
        var server = new RabbitServer();
        await using (var model = new FakeModel(server))
        {
            await model.QueueDeclareAsync("my_queue");
            var expectedConsumerTag = "foo";

            var consumer = new FakeAsyncDefaultBasicConsumer(model);

            await model.BasicConsumeAsync("my_queue", false, expectedConsumerTag, consumer);
            Assert.True(consumer.IsRunning);
            await model.BasicCancelAsync(expectedConsumerTag);
            var result = await consumer.LastCancelOkConsumerTag.Task;
            Assert.False(consumer.IsRunning);

            Assert.Equal(expectedConsumerTag, result);
        }
    }

    [Fact]
    public async Task BasicConsume_works_for_asynchronous_consumers()
    {
        var server = new RabbitServer();
        await using (var model = new FakeModel(server))
        {
            await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
            await model.QueueDeclareAsync("my_queue");
            await model.ExchangeBindAsync("my_queue", "my_exchange", null);

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);
            await model.BasicPublishAsync("my_exchange", null, encodedMessage);

            var consumer = new FakeAsyncDefaultBasicConsumer(model);
            await model.BasicConsumeAsync("my_queue", false, consumer);
            Assert.True(consumer.IsRunning);

            var (_, _, _, _, _, _, body) = await consumer.LastDelivery.Task;
            Assert.Equal(encodedMessage, body);
        }
    }

    [Fact]
    public async Task BasicConsume_generates_consumer_tag_if_none_specified()
    {
        var server = new RabbitServer();
        await using (var model = new FakeModel(server))
        {
            await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
            await model.QueueDeclareAsync("my_queue");
            await model.ExchangeBindAsync("my_queue", "my_exchange", null);

            var consumer = new FakeAsyncDefaultBasicConsumer(model);
            var consumerTag = await model.BasicConsumeAsync("my_queue", false, "", false, false, null, consumer);

            Assert.NotNull(consumerTag);
            Assert.NotEmpty(consumerTag);
        }
    }

    [Fact]
    public async Task BasicConsume_with_duplicate_consumer_tag_fails()
    {
        var server = new RabbitServer();
        await using (var model = new FakeModel(server))
        {
            await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
            await model.QueueDeclareAsync("my_queue");
            await model.ExchangeBindAsync("my_queue", "my_exchange", null);

            var consumerTag = "foo";
            var consumer = new FakeAsyncDefaultBasicConsumer(model);
            await model.BasicConsumeAsync("my_queue", false, consumerTag, consumer);
            
            await Assert.ThrowsAsync<OperationInterruptedException>(() =>
                model.BasicConsumeAsync("my_queue", false, consumerTag, consumer));
        }
    }

    [Fact]
    public async Task BasicGet_retrieves_message_from_the_queue()
    {
        var server = new RabbitServer();
        await using (var model = new FakeModel(server))
        {
            await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
            await model.QueueDeclareAsync("my_queue");
            await model.ExchangeBindAsync("my_queue", "my_exchange", null);

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);
            await model.BasicPublishAsync("my_exchange", null, false, new BasicProperties(), encodedMessage);

            var response = await model.BasicGetAsync("my_queue", false);

            Assert.Equal(encodedMessage, response.Body.ToArray());
            Assert.True(response.DeliveryTag > 0ul);
        }
    }

    [Fact]
    public async Task BasicGet_with_no_messages_returns_null()
    {
        var server = new RabbitServer();
        await using (var model = new FakeModel(server))
        {
            await model.QueueDeclareAsync("my_queue");
            var response = await model.BasicGetAsync("my_queue", false);
            Assert.Null(response);
        }
    }

    [Fact]
    public async Task BasicGet_with_no_queue_returns_null()
    {
        var server = new RabbitServer();
        await using (var model = new FakeModel(server))
        {
            var response = await model.BasicGetAsync("my_queue", false);
            Assert.Null(response);
        }
    }

    [Theory]
    [InlineData(true, 0)] // BasicGet WITH auto-ack SHOULD remove the message from the queue
    [InlineData(false, 1)] // BasicGet with NO auto-ack should NOT remove the message from the queue
    public async Task BasicGet_does_not_remove_the_message_from_the_queue_if_not_acked(bool autoAck, int expectedMessageCount)
    {
        var server = new RabbitServer();
        await using (var model = new FakeModel(server))
        {
            await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
            await model.QueueDeclareAsync("my_queue");
            await model.ExchangeBindAsync("my_queue", "my_exchange", null);

            var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
            await model.BasicPublishAsync("my_exchange", null, encodedMessage);

            _ = await model.BasicGetAsync("my_queue", autoAck);

            Assert.Equal(expectedMessageCount, server.Queues["my_queue"].MessageCount);
            Assert.Equal(expectedMessageCount, model.WorkingMessagesForUnitTests.Count);
        }
    }

    [Theory]
    [InlineData(true, 1)] // If requeue param to BasicNack is true, the message that is nacked should remain in Rabbit
    [InlineData(false, 0)] // If requeue param to BasicNack is false, the message that is nacked should be removed from Rabbit
    public async Task BasicNack_does_not_reenqueue_a_brand_new_message(bool requeue, int expectedMessageCount)
    {
        var server = new RabbitServer();
        await using (var model = new FakeModel(server))
        {
            await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
            await model.QueueDeclareAsync("my_queue");
            await model.ExchangeBindAsync("my_queue", "my_exchange", null);

            var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
            await model.BasicPublishAsync("my_exchange", null, encodedMessage);

            var consumer = new AsyncEventingBasicConsumer(model);
            using (var messageProcessed = new ManualResetEventSlim())
            {
                consumer.ReceivedAsync += (_, _) =>
                {
                    messageProcessed.Set();
                    return Task.CompletedTask;
                };

                await model.BasicConsumeAsync("my_queue", false, consumer);
                Assert.True(consumer.IsRunning);

                messageProcessed.Wait();
                var deliveryTag = model.WorkingMessagesForUnitTests.First().Key;
                await model.BasicNackAsync(deliveryTag, false, requeue);

                Assert.Equal(expectedMessageCount, server.Queues["my_queue"].MessageCount);
                Assert.Equal(expectedMessageCount, model.WorkingMessagesForUnitTests.Count);
            }
        }
    }

    [Theory]
    [InlineData(false, 0)] // If requeue param to BasicNack is false, the message that is nacked should be removed from Rabbit
    [InlineData(true, 1)] // If requeue param to BasicNack is true, the message that is nacked should remain in Rabbit
    public async Task BasicReject_does_not_reenqueue_a_brand_new_message(bool requeue, int expectedMessageCount)
    {
        var server = new RabbitServer();
        await using var model = new FakeModel(server);

        await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
        await model.QueueDeclareAsync("my_queue");
        await model.ExchangeBindAsync("my_queue", "my_exchange", null);

        var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
        await model.BasicPublishAsync("my_exchange", null, encodedMessage);

        var consumer = new AsyncEventingBasicConsumer(model);
        using var messageProcessed = new ManualResetEventSlim();

        consumer.ReceivedAsync += (_, _) =>
        {
            messageProcessed.Set();
            return Task.CompletedTask;
        };

        await model.BasicConsumeAsync("my_queue", false, consumer);
        Assert.True(consumer.IsRunning);

        messageProcessed.Wait();
        var deliveryTag = model.WorkingMessagesForUnitTests.First().Key;
        await model.BasicRejectAsync(deliveryTag, requeue);

        Assert.Equal(expectedMessageCount, server.Queues["my_queue"].MessageCount);
        Assert.Equal(expectedMessageCount, model.WorkingMessagesForUnitTests.Count);
    }

    [Fact]
    public async Task BasicPublish_publishes_message()
    {
        var server = new RabbitServer();
        await using (var model = new FakeModel(server))
        {
            await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
            await model.QueueDeclareAsync("my_queue");
            await model.ExchangeBindAsync("my_queue", "my_exchange", null);

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);

            await model.BasicPublishAsync("my_exchange", null, encodedMessage);

            Assert.Equal(1, server.Queues["my_queue"].MessageCount);
            if (!server.Queues["my_queue"].TryPeekForUnitTests(out var peeked))
                Assert.Fail("No message in queue");
            else
                Assert.Equal(encodedMessage, peeked.Body);
        }
    }

    [Fact]
    public async Task BasicPublish_to_default_exchange_publishes_message()
    {
        var server = new RabbitServer();
        await using (var model = new FakeModel(server))
        {
            await model.QueueDeclareAsync("my_queue");

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);

            await model.BasicPublishAsync("", "my_queue", encodedMessage);

            Assert.Equal(1, server.Queues["my_queue"].MessageCount);
            if (!server.Queues["my_queue"].TryPeekForUnitTests(out var peeked))
                Assert.Fail("No message in queue");
            else
                Assert.Equal(encodedMessage, peeked.Body);
        }
    }

    [Fact]
    public async Task BasicPublish_before_BasicConsume_does_not_deadlock()
    {
        var server = new RabbitServer();
        await using (var model = new FakeModel(server))
        {
            await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
            await model.QueueDeclareAsync("my_queue");
            await model.ExchangeBindAsync("my_queue", "my_exchange", null);

            var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
            await model.BasicPublishAsync("my_exchange", null, encodedMessage);

            using (var deadlockDetector = new ManualResetEventSlim())
            using (var allowedThrough = new ManualResetEventSlim())
            {
                var consumer = new AsyncEventingBasicConsumer(model);
                consumer.ReceivedAsync += (_, _) =>
                {
                    if (deadlockDetector.Wait(10000))
                    {
                        allowedThrough.Set();
                    }

                    return Task.CompletedTask;
                };

                await model.BasicConsumeAsync("my_queue", false, consumer);

                deadlockDetector.Set();
                var wasAllowedThrough = allowedThrough.Wait(10000);
                Assert.True(wasAllowedThrough);
            }
        }
    }

    [Fact]
    public async Task BasicPublish_after_BasicConsume_does_not_deadlock()
    {
        var server = new RabbitServer();
        await using (var model = new FakeModel(server))
        {
            await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
            await model.QueueDeclareAsync("my_queue");
            await model.ExchangeBindAsync("my_queue", "my_exchange", null);

            using (var deadlockDetector = new ManualResetEventSlim())
            using (var allowedThrough = new ManualResetEventSlim())
            {
                var consumer = new AsyncEventingBasicConsumer(model);
                consumer.ReceivedAsync += (_, _) =>
                {
                    if (deadlockDetector.Wait(10000))
                    {
                        allowedThrough.Set();
                    }

                    return Task.CompletedTask;
                };

                await model.BasicConsumeAsync("my_queue", false, consumer);

                var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
                await model.BasicPublishAsync("my_exchange", null, encodedMessage);

                deadlockDetector.Set();
                var wasAllowedThrough = allowedThrough.Wait(10000);
                Assert.True(wasAllowedThrough);
            }
        }
    }

    [Fact]
    public async Task BasicPublish_after_BasicConsume_with_BlockingDelivery_is_synchronous()
    {
        var server = new RabbitServer(blockingDeliveryMode: true);
        using var model = new FakeModel(server);

        await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
        await model.QueueDeclareAsync("my_queue");
        await model.ExchangeBindAsync("my_queue", "my_exchange", null);

        var trackedValue = "initial";

        var consumer = new AsyncEventingBasicConsumer(model);
        consumer.ReceivedAsync += (_, _) =>
        {
            trackedValue = "from_consumer";

            return Task.CompletedTask;
        };

        await model.BasicConsumeAsync("my_queue", false, consumer);

        var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
        await model.BasicPublishAsync("my_exchange", null, encodedMessage);

        trackedValue.Should().Be("from_consumer");
    }

    [Fact]
    public async Task BasicPublish_before_BasicConsume_with_BlockingDelivery_is_synchronous()
    {
        var server = new RabbitServer(blockingDeliveryMode: true);
        await using var model = new FakeModel(server);

        await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
        await model.QueueDeclareAsync("my_queue");
        await model.ExchangeBindAsync("my_queue", "my_exchange", null);

        var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
        await model.BasicPublishAsync("my_exchange", null, encodedMessage);

        var trackedValue = "initial";

        var consumer = new AsyncEventingBasicConsumer(model);
        consumer.ReceivedAsync += (_, _) =>
        {
            trackedValue = "from_consumer";

            return Task.CompletedTask;
        };

        await model.BasicConsumeAsync("my_queue", false, consumer);

        trackedValue.Should().Be("from_consumer");
    }

    [Fact]
    public async Task BasicQos_does_nothing_because_it_is_not_implemented_yet()
    {
        var server = new RabbitServer();
        await using (var model = new FakeModel(server))
            await model.BasicQosAsync(1u, 1, true);

        Assert.True(true);
    }
}
