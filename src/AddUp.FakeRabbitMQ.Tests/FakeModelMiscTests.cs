using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace AddUp.RabbitMQ.Fakes;

[ExcludeFromCodeCoverage]
public class FakeModelMiscTests
{
    [Fact]
    public void CreateBasicProperties_returns_basic_properties()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            var result = new BasicProperties();
            Assert.NotNull(result);
        }
    }

    [Fact]
    public async Task MessageCount_is_zero_when_queue_is_just_created()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string queueName = "myQueue";
            await model.QueueDeclareAsync(queueName);
            Assert.Equal(0u, await model.MessageCountAsync(queueName));
        }
    }

    [Fact]
    public async Task MessageCount_returns_the_number_of_messages_in_the_queue()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string queueName = "myQueue";
            await model.QueueDeclareAsync(queueName);
            await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
            await model.ExchangeBindAsync(queueName, "my_exchange", null);

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);
            await model.BasicPublishAsync("my_exchange", null, encodedMessage);

            Assert.Equal(1u, await model.MessageCountAsync(queueName));
        }
    }

    [Fact]
    public async Task MessageCount_returns_the_number_of_non_consumed_messages_in_the_queue()
    {
        var server = new RabbitServer();
        using var model = new FakeModel(server);

        const string queueName = "myQueue";
        await model.QueueDeclareAsync(queueName);
        await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
        await model.ExchangeBindAsync(queueName, "my_exchange", null);

        for (var i = 0; i < 10; i++)
        {
            var message = $"hello world: {i}";
            var encodedMessage = Encoding.ASCII.GetBytes(message);
            await model.BasicPublishAsync("my_exchange", null, encodedMessage);
        }

        // Consume 4 messages
        const string consumerTag = "consumer-tag";
        var consumer = new AsyncEventingBasicConsumer(model);
        var consumptionCount = 0;
        using var messagesProcessed = new ManualResetEventSlim();

        consumer.ReceivedAsync += async (s, e) =>
        {
            consumptionCount++;
            if (consumptionCount > 4) return;

            await model.BasicAckAsync(e.DeliveryTag, false);
            if (consumptionCount == 4)
                messagesProcessed.Set();
        };

        await model.BasicConsumeAsync(queueName, false, consumerTag, consumer);

        messagesProcessed.Wait();
        Assert.Equal(6u, await model.MessageCountAsync(queueName));
    }

    [Fact]
    public async Task MessageCount_returns_the_number_of_non_consumed_messages_in_the_queue_autoAck_mode()
    {
        var server = new RabbitServer();
        using var model = new FakeModel(server);

        const string queueName = "myQueue";
        await model.QueueDeclareAsync(queueName);
        await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
        await model.ExchangeBindAsync(queueName, "my_exchange", null);

        async Task publishMessages(int startIndex, int count)
        {
            for (var i = startIndex; i < startIndex + count; i++)
            {
                var message = $"hello world: {i}";
                var encodedMessage = Encoding.ASCII.GetBytes(message);
                await model.BasicPublishAsync("my_exchange", null, encodedMessage);
            }
        }

        await publishMessages(0, 4);

        // Consume 4 messages
        const string consumerTag = "consumer-tag";
        var consumer = new AsyncEventingBasicConsumer(model);
        var consumptionCount = 0;
        using var messagesProcessed = new ManualResetEventSlim();

        Task consume(object sender, BasicDeliverEventArgs e)
        {
            consumptionCount++;
            if (consumptionCount >= 4)
                messagesProcessed.Set();

            return Task.CompletedTask;
        }

        consumer.ReceivedAsync += consume;

        await model.BasicConsumeAsync(queueName, true, consumerTag, consumer);
        messagesProcessed.Wait();
        await model.BasicCancelAsync(consumerTag);

        await publishMessages(4, 6); // Publish another 6 messages
        await Task.Delay(1000); // They will never be consumed

        Assert.Equal(4, consumptionCount);
        Assert.Equal(6u, await model.MessageCountAsync(queueName));
    }

    [Fact]
    public async Task ConsumerCount_is_zero_when_queue_is_just_created()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string queueName = "myQueue";
            await model.QueueDeclareAsync(queueName);
            Assert.Equal(0u, await model.ConsumerCountAsync(queueName));
        }
    }

    [Fact]
    public async Task ConsumerCount_returns_the_number_of_attached_consumers()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string queueName = "myQueue";
            await model.QueueDeclareAsync(queueName);
            await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
            await model.ExchangeBindAsync(queueName, "my_exchange", null);

            // Attach 2 consumers
            await model.BasicConsumeAsync(queueName, true, new AsyncDefaultBasicConsumer(model));
            await model.BasicConsumeAsync(queueName, true, new AsyncDefaultBasicConsumer(model));

            Assert.Equal(2u, await model.ConsumerCountAsync(queueName));
        }
    }
}