using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace AddUp.RabbitMQ.Fakes;

[ExcludeFromCodeCoverage]
public class FakeModelExchangeTests
{
    [Fact]
    public async Task ExchangeDeclare_with_all_arguments_creates_exchange()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string exchangeName = "someExchange";
            const string exchangeType = "someType";
            const bool isDurable = true;
            const bool isAutoDelete = false;
            var arguments = new Dictionary<string, object>();

            await model.ExchangeDeclareAsync(exchange: exchangeName, type: exchangeType, durable: isDurable, autoDelete: isAutoDelete, arguments: arguments);
            Assert.Equal(2, server.Exchanges.Count);
            Assert.Single(server.Exchanges.Where(x => x.Key == exchangeName));

            var exchange = server.Exchanges.Single(x => x.Key == exchangeName);
            AssertEx.AssertExchangeDetails(exchange, exchangeName, isAutoDelete, arguments, isDurable, exchangeType);
        }
    }

    [Fact]
    public async Task ExchangeDeclare_with_name_type_and_durable_creates_exchange()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string exchangeName = "someExchange";
            const string exchangeType = "someType";
            const bool isDurable = true;

            await model.ExchangeDeclareAsync(exchange: exchangeName, type: exchangeType, durable: isDurable);
            Assert.Equal(2, server.Exchanges.Count);
            Assert.Single(server.Exchanges.Where(x => x.Key == exchangeName));

            var exchange = server.Exchanges.Single(x => x.Key == exchangeName);
            AssertEx.AssertExchangeDetails(exchange, exchangeName, false, null, isDurable, exchangeType);
        }
    }

    [Fact]
    public async Task ExchangeDeclare_with_name_and_type_creates_exchange()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string exchangeName = "someExchange";
            const string exchangeType = "someType";

            await model.ExchangeDeclareAsync(exchange: exchangeName, type: exchangeType);
            Assert.Equal(2, server.Exchanges.Count);
            Assert.Single(server.Exchanges.Where(x => x.Key == exchangeName));

            var exchange = server.Exchanges.Single(x => x.Key == exchangeName);
            AssertEx.AssertExchangeDetails(exchange, exchangeName, false, null, false, exchangeType);
        }
    }

    [Fact]
    public async Task ExchangeDeclarePassive_does_not_throw_if_exchange_exists()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string exchangeName = "someExchange";
            await model.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Direct);
            await model.ExchangeDeclarePassiveAsync(exchange: exchangeName);
        }

        Assert.True(true); // The test is successful if it does not throw
    }

    [Fact]
    public async Task ExchangeDeclarePassive_throws_if_exchange_does_not_exist()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string exchangeName = "someExchange";
            await Assert.ThrowsAsync<OperationInterruptedException>(
                () => model.ExchangeDeclarePassiveAsync(exchange: exchangeName));
        }
    }

    [Fact]
    public async Task ExchangeDeclareNoWait_creates_exchange()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string exchangeName = "someExchange";
            const string exchangeType = "someType";
            const bool isDurable = true;
            const bool isAutoDelete = false;
            var arguments = new Dictionary<string, object>();

            await model.ExchangeDeclareAsync(exchange: exchangeName, noWait: true, type: exchangeType, durable: isDurable, autoDelete: isAutoDelete, arguments: arguments);
            Assert.Equal(2, server.Exchanges.Count);
            Assert.Single(server.Exchanges.Where(x => x.Key == exchangeName));

            var exchange = server.Exchanges.Single(x => x.Key == exchangeName);
            AssertEx.AssertExchangeDetails(exchange, exchangeName, isAutoDelete, arguments, isDurable, exchangeType);
        }
    }

    [Fact]
    public async Task ExchangeDelete_with_only_name_argument_removes_the_exchange_if_it_exists()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string exchangeName = "someExchange";
            await model.ExchangeDeclareAsync(exchangeName, "someType");
            await model.ExchangeDeleteAsync(exchange: exchangeName);
            Assert.Single(server.Exchanges);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExchangeDelete_removes_the_exchange_if_it_exists(bool ifUnused)
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string exchangeName = "someExchange";
            await model.ExchangeDeclareAsync(exchangeName, "someType");
            await model.ExchangeDeleteAsync(exchange: exchangeName, ifUnused: ifUnused);
            Assert.Single(server.Exchanges);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExchangeDeleteNoWait_removes_the_exchange_if_it_exists(bool ifUnused)
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string exchangeName = "someExchange";
            await model.ExchangeDeclareAsync(exchangeName, "someType");
            await model.ExchangeDeleteAsync(exchange: exchangeName, noWait: true, ifUnused: ifUnused);
            Assert.Single(server.Exchanges);
        }
    }

    [Fact]
    public async Task ExchangeDelete_does_nothing_if_exchange_does_not_exist()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
        {
            const string exchangeName = "someExchange";
            await model.ExchangeDeclareAsync(exchangeName, "someType");
            await model.ExchangeDeleteAsync(exchange: "someOtherExchange");
            Assert.Equal(2, server.Exchanges.Count);
            Assert.Single(server.Exchanges.Where(s => s.Key == exchangeName));
        }
    }

    [Fact]
    public async Task ExchangeBind_binds_an_exchange_to_a_queue()
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
            await model.ExchangeBindAsync(queueName, exchangeName, routingKey, arguments);
            AssertEx.AssertBinding(server, exchangeName, routingKey, queueName);
        }
    }

    [Fact]
    public async Task ExchangeBindNoWait_binds_an_exchange_to_a_queue()
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
            await model.ExchangeBindAsync(queueName, exchangeName, routingKey, arguments, noWait: true);
            AssertEx.AssertBinding(server, exchangeName, routingKey, queueName);
        }
    }

    [Fact]
    public async Task ExchangeUnbind_removes_binding()
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
            await model.ExchangeUnbindAsync(queueName, exchangeName, routingKey, arguments);

            Assert.True(server.Exchanges[exchangeName].Bindings.IsEmpty);
            Assert.Single(server.Queues[queueName].Bindings);
        }
    }

    [Fact]
    public async Task ExchangeUnbindNoWait_removes_binding()
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
            await model.ExchangeUnbindAsync(queueName, exchangeName, routingKey, arguments, noWait: true);

            Assert.True(server.Exchanges[exchangeName].Bindings.IsEmpty);
            Assert.Single(server.Queues[queueName].Bindings);
        }
    }
}
