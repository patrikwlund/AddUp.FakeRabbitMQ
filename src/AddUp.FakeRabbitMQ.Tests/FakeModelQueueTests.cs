using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace AddUp.RabbitMQ.Fakes;

[ExcludeFromCodeCoverage]
public class FakeModelQueueTests
{
    private long lastDeliveryTag; // USed to simulate generation of the delivery tag by FakeModel

    [Fact]
    public async Task QueueBind_binds_an_exchange_to_a_queue()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string queueName = "someQueue";
            const string exchangeName = "someExchange";
            const string routingKey = "someRoutingKey";
            var arguments = new Dictionary<string, object>();

            await model.ExchangeDeclareAsync(exchangeName, "direct");
            await model.QueueDeclareAsync(queueName);
            await model.QueueBindAsync(queueName, exchangeName, routingKey, arguments);

            AssertEx.AssertBinding(server, exchangeName, routingKey, queueName);
        }
    }

    [Fact]
    public async Task QueueBindNoWait_binds_an_exchange_to_a_queue()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string queueName = "someQueue";
            const string exchangeName = "someExchange";
            const string routingKey = "someRoutingKey";
            var arguments = new Dictionary<string, object>();

            await model.ExchangeDeclareAsync(exchangeName, "direct");
            await model.QueueDeclareAsync(queueName);
            await model.QueueBindAsync(queueName, exchangeName, routingKey, arguments, noWait: true);

            AssertEx.AssertBinding(server, exchangeName, routingKey, queueName);
        }
    }

    [Fact]
    public async Task QueueUnbind_removes_binding()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string queueName = "someQueue";
            const string exchangeName = "someExchange";
            const string routingKey = "someRoutingKey";
            var arguments = new Dictionary<string, object>();

            await model.ExchangeDeclareAsync(exchangeName, "direct");
            await model.QueueDeclareAsync(queueName);
            await model.ExchangeBindAsync(exchangeName, queueName, routingKey, arguments);
            await model.QueueUnbindAsync(queueName, exchangeName, routingKey, arguments);

            Assert.True(server.Exchanges[exchangeName].Bindings.IsEmpty);
            Assert.Single(server.Queues[queueName].Bindings);
        }
    }

    [Fact]
    public async Task QueueDeclare_without_arguments_creates_a_queue()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            await model.QueueDeclareAsync();
            Assert.Single(server.Queues);
        }
    }

    [Fact]
    public async Task QueueDeclarePassive_does_not_throw_if_queue_exists()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string queueName = "myQueue";
            await model.QueueDeclareAsync(queueName);
            await model.QueueDeclarePassiveAsync(queueName);
        }

        Assert.True(true); // The test is successful if it does not throw
    }

    [Fact]
    public async Task QueueDeclarePassive_throws_if_queue_does_not_exist()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string queueName = "myQueue";
            await Assert.ThrowsAsync<OperationInterruptedException>(() => model.QueueDeclarePassiveAsync(queueName));
        }
    }

    [Fact]
    public async Task QueueDeclare_creates_a_queue()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string queueName = "someQueue";
            const bool isDurable = true;
            const bool isExclusive = true;
            const bool isAutoDelete = false;
            var arguments = new Dictionary<string, object>();

            await model.QueueDeclareAsync(queue: queueName, durable: isDurable, exclusive: isExclusive, autoDelete: isAutoDelete, arguments: arguments);
            Assert.Single(server.Queues);

            var queue = server.Queues.First();
            AssertEx.AssertQueueDetails(queue, queueName, isAutoDelete, arguments, isDurable, isExclusive);
        }
    }

    [Fact]
    public async Task QueueDeclareoWait_creates_a_queue()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string queueName = "someQueue";
            const bool isDurable = true;
            const bool isExclusive = true;
            const bool isAutoDelete = false;
            var arguments = new Dictionary<string, object>();

            await model.QueueDeclareAsync(queue: queueName, durable: isDurable, exclusive: isExclusive, autoDelete: isAutoDelete, arguments: arguments, noWait: true);
            Assert.Single(server.Queues);

            var queue = server.Queues.First();
            AssertEx.AssertQueueDetails(queue, queueName, isAutoDelete, arguments, isDurable, isExclusive);
        }
    }

    [Fact]
    public async Task QueueDelete_with_onlyèname_argument_deletes_the_queue()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string queueName = "someName";
            await model.QueueDeclareAsync(queueName, true, true, true, null);
            await model.QueueDeleteAsync(queueName);
            Assert.True(server.Queues.IsEmpty);
        }
    }

    [Fact]
    public async Task QueueDelete_with_arguments_deletes_the_queue()
    {
        var node = new RabbitServer();
        using (var model = new FakeModel(node))
        {
            const string queueName = "someName";
            await model.QueueDeclareAsync(queueName, true, true, true, null);
            await model.QueueDeleteAsync(queueName, true, true);
            Assert.True(node.Queues.IsEmpty);
        }
    }

    [Fact]
    public async Task QueueDeleteNoWait_with_arguments_deletes_the_queue()
    {
        var node = new RabbitServer();
        using (var model = new FakeModel(node))
        {
            const string queueName = "someName";
            await model.QueueDeclareAsync(queueName, true, true, true, null);
            await model.QueueDeleteAsync(queueName, true, true, noWait: true);
            Assert.True(node.Queues.IsEmpty);
        }
    }

    [Fact]
    public async Task QueueDelete_does_nothing_if_queue_does_not_exist()
    {
        var node = new RabbitServer();
        using (var model = new FakeModel(node))
        {
            await model.QueueDeleteAsync("someQueue");
            Assert.True(node.Queues.IsEmpty);
        }
    }

    [Fact]
    public async Task QueuePurge_removes_all_messages_from_specified_queue()
    {
        var node = new RabbitServer();
        using (var model = new FakeModel(node))
        {
            await model.QueueDeclareAsync("my_other_queue");
            node.Queues["my_other_queue"].Enqueue(MakeRabbitMessage());
            node.Queues["my_other_queue"].Enqueue(MakeRabbitMessage());

            await model.QueueDeclareAsync("my_queue");
            node.Queues["my_queue"].Enqueue(MakeRabbitMessage());
            node.Queues["my_queue"].Enqueue(MakeRabbitMessage());
            node.Queues["my_queue"].Enqueue(MakeRabbitMessage());
            node.Queues["my_queue"].Enqueue(MakeRabbitMessage());

            var count = await model.QueuePurgeAsync("my_queue");
            Assert.Equal(4u, count);

            Assert.False(node.Queues["my_queue"].HasMessages);
            Assert.True(node.Queues["my_other_queue"].HasMessages);
        }
    }

    [Fact]
    public async Task QueuePurge_returns_0_if_queue_does_not_exist()
    {
        var node = new RabbitServer();
        using (var model = new FakeModel(node))
        {
            await model.QueueDeclareAsync("my_queue");
            node.Queues["my_queue"].Enqueue(MakeRabbitMessage());
            node.Queues["my_queue"].Enqueue(MakeRabbitMessage());
            node.Queues["my_queue"].Enqueue(MakeRabbitMessage());
            node.Queues["my_queue"].Enqueue(MakeRabbitMessage());

            var count = await model.QueuePurgeAsync("my_other_queue");
            Assert.Equal(0u, count);

            Assert.True(node.Queues["my_queue"].HasMessages);
        }
    }

    private RabbitMessage MakeRabbitMessage()
    {
        _ = Interlocked.Increment(ref lastDeliveryTag);
        var deliveryTag = Convert.ToUInt64(lastDeliveryTag);
        return new(deliveryTag);
    }
}
