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
    public async Task Close_closes_the_channel()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            await model.CloseAsync();

            Assert.True(model.IsClosed);
            Assert.False(model.IsOpen);
            Assert.NotNull(model.CloseReason);
        }
    }

    [Fact]
    public async Task Close_with_arguments_closes_the_channel()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            await model.CloseAsync(5, "some message");

            Assert.True(model.IsClosed);
            Assert.False(model.IsOpen);
            Assert.NotNull(model.CloseReason);
        }
    }

    [Fact]
    public async Task BasicClose_unregisters_consumers()
    {
        var server = new RabbitServer();
        var model = new FakeModel(server);
        await model.QueueDeclareAsync("my_queue");
        var expectedConsumerTag = "foo";
        var actualConsumerTag = "";

        var consumer = new AsyncEventingBasicConsumer(model);
        var receivedHasRan = false;
        consumer.ReceivedAsync += (s, e) => { receivedHasRan = true; return Task.CompletedTask; };
        consumer.UnregisteredAsync += (s, e) => { actualConsumerTag = e.ConsumerTags[0]; return Task.CompletedTask; };

        await model.BasicConsumeAsync("my_queue", false, expectedConsumerTag, consumer);
        Assert.True(consumer.IsRunning);

        await model.CloseAsync();
        await model.DisposeAsync();

        Assert.False(consumer.IsRunning);

        using (var sendModel = new FakeModel(server))
        {
            await model.BasicPublishAsync("", "my_queue", Encoding.ASCII.GetBytes("hello"));
        }

        Assert.Equal(expectedConsumerTag, actualConsumerTag);
        Assert.Equal(0, server.Queues["my_queue"].ConsumerCount);
        Assert.False(receivedHasRan);
    }

    [Fact]
    public async Task BasicClose_only_fires_model_shutdown_once()
    {
        var server = new RabbitServer();
        var model = new FakeModel(server);
        var shutdownCount = 0;
        model.ChannelShutdownAsync += (o, ea) => { shutdownCount++; return Task.CompletedTask; };
        await model.CloseAsync();
        await model.CloseAsync();
        await model.CloseAsync();
        Assert.Equal(1, shutdownCount);
    }

    [Fact]
    public async Task BasicClose_does_not_deadlock_when_invoked_from_receive()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            await model.ExchangeDeclareAsync("my_exchange", ExchangeType.Direct);
            await model.QueueDeclareAsync("my_queue");
            await model.ExchangeBindAsync("my_queue", "my_exchange", null);

            var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
            await model.BasicPublishAsync("my_exchange", null, encodedMessage);

            using (var allowedThrough = new ManualResetEventSlim())
            {
                var consumer = new AsyncEventingBasicConsumer(model);
                consumer.ReceivedAsync += async (_, _) =>
                {
                    await model.CloseAsync();
                    allowedThrough.Set();
                };

                await model.BasicConsumeAsync("my_queue", false, consumer);

                var wasAllowedThrough = allowedThrough.Wait(10000);
                Assert.True(wasAllowedThrough);
            }
        }
    }

    [Fact]
    public async Task Abort_closes_the_channel()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            await model.AbortAsync();

            Assert.True(model.IsClosed);
            Assert.False(model.IsOpen);
            Assert.NotNull(model.CloseReason);
        }
    }
}
