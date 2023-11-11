using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
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
            model.BasicPublish("my_exchange", null, model.CreateBasicProperties(), encodedMessage);

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
                model.BasicPublish("my_exchange", null, model.CreateBasicProperties(), encodedMessage);
            }

            // Consume 4 messages
            var consumer = new EventingBasicConsumer(model);
            var consumptionCount = 0;
            using (var messagesProcessed = new ManualResetEventSlim())
            {
                consumer.Received += (s, e) =>
                {
                    if (consumptionCount >= 4)
                    {
                        messagesProcessed.Set();
                        return;
                    }

                    model.BasicAck(e.DeliveryTag, false);
                    consumptionCount++;
                };

                model.BasicConsume(queueName, true, consumer);
                messagesProcessed.Wait();
                Assert.Equal(6u, model.MessageCount(queueName));
            }
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
}