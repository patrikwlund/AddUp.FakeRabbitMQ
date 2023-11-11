using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace AddUp.RabbitMQ.Fakes;

[ExcludeFromCodeCoverage]
public class FakeModelAbortAndCloseTests
{
    [Fact]
    public void Close_closes_the_channel()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            model.Close();

            Assert.True(model.IsClosed);
            Assert.False(model.IsOpen);
            Assert.NotNull(model.CloseReason);
        }
    }

    [Fact]
    public void Close_with_arguments_closes_the_channel()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            model.Close(5, "some message");

            Assert.True(model.IsClosed);
            Assert.False(model.IsOpen);
            Assert.NotNull(model.CloseReason);
        }
    }

    [Fact]
    public void BasicClose_unregisters_consumers()
    {
        var server = new RabbitServer();
        var model = new FakeModel(server);
        model.QueueDeclare("my_queue");
        var expectedConsumerTag = "foo";
        var actualConsumerTag = "";

        var consumer = new EventingBasicConsumer(model);
        var receivedHasRan = false;
        consumer.Received += (s, e) => receivedHasRan = true;
        consumer.Unregistered += (s, e) => actualConsumerTag = e.ConsumerTags[0];

        model.BasicConsume("my_queue", false, expectedConsumerTag, consumer);
        Assert.True(consumer.IsRunning);

        model.Close();
        model.Dispose();

        Assert.False(consumer.IsRunning);

        using (var sendModel = new FakeModel(server))
        {
            model.BasicPublish("", "my_queue", model.CreateBasicProperties(), Encoding.ASCII.GetBytes("hello"));
        }

        Assert.Equal(expectedConsumerTag, actualConsumerTag);
        Assert.Equal(0, server.Queues["my_queue"].ConsumerCount);
        Assert.False(receivedHasRan);
    }

    [Fact]
    public void BasicClose_only_fires_model_shutdown_once()
    {
        var server = new RabbitServer();
        var model = new FakeModel(server);
        var shutdownCount = 0;
        model.ModelShutdown += (o, ea) => ++shutdownCount;
        model.Close();
        model.Close();
        model.Close();
        Assert.Equal(1, shutdownCount);
    }

    [Fact]
    public void BasicClose_does_not_deadlock_when_invoked_from_receive()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            model.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            model.QueueDeclare("my_queue");
            model.ExchangeBind("my_queue", "my_exchange", null);

            var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
            model.BasicPublish("my_exchange", null, model.CreateBasicProperties(), encodedMessage);

            using (var allowedThrough = new ManualResetEventSlim())
            {
                var consumer = new EventingBasicConsumer(model);
                consumer.Received += (_, _) =>
                {
                    model.Close();
                    allowedThrough.Set();
                };

                model.BasicConsume("my_queue", false, consumer);

                var wasAllowedThrough = allowedThrough.Wait(10000);
                Assert.True(wasAllowedThrough);
            }
        }
    }

    [Fact]
    public void Abort_closes_the_channel()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            model.Abort();

            Assert.True(model.IsClosed);
            Assert.False(model.IsOpen);
            Assert.NotNull(model.CloseReason);
        }
    }

    [Fact]
    public void Abort_with_arguments_closes_the_channel()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            model.Abort(5, "some message");

            Assert.True(model.IsClosed);
            Assert.False(model.IsOpen);
            Assert.NotNull(model.CloseReason);
        }
    }
}
