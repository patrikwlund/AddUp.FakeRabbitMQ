using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace AddUp.RabbitMQ.Fakes
{
    [ExcludeFromCodeCoverage]
    public class FakeModelExchangeTests
    {
        [Fact]
        public void ExchangeDeclare_with_all_arguments_creates_exchange()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string exchangeName = "someExchange";
                const string exchangeType = "someType";
                const bool isDurable = true;
                const bool isAutoDelete = false;
                var arguments = new Dictionary<string, object>();

                model.ExchangeDeclare(exchange: exchangeName, type: exchangeType, durable: isDurable, autoDelete: isAutoDelete, arguments: arguments);
                Assert.Single(server.Exchanges);

                var exchange = server.Exchanges.First();
                AssertEx.AssertExchangeDetails(exchange, exchangeName, isAutoDelete, arguments, isDurable, exchangeType);
            }
        }

        [Fact]
        public void ExchangeDeclare_with_name_type_and_durable_creates_exchange()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string exchangeName = "someExchange";
                const string exchangeType = "someType";
                const bool isDurable = true;

                model.ExchangeDeclare(exchange: exchangeName, type: exchangeType, durable: isDurable);
                Assert.Single(server.Exchanges);

                var exchange = server.Exchanges.First();
                AssertEx.AssertExchangeDetails(exchange, exchangeName, false, null, isDurable, exchangeType);
            }
        }

        [Fact]
        public void ExchangeDeclare_with_name_and_type_creates_exchange()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string exchangeName = "someExchange";
                const string exchangeType = "someType";

                model.ExchangeDeclare(exchange: exchangeName, type: exchangeType);
                Assert.Single(server.Exchanges);

                var exchange = server.Exchanges.First();
                AssertEx.AssertExchangeDetails(exchange, exchangeName, false, null, false, exchangeType);
            }
        }

        [Fact]
        public void ExchangeDeclarePassive_does_not_throw_if_exchange_exists()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string exchangeName = "someExchange";
                model.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Direct);
                model.ExchangeDeclarePassive(exchange: exchangeName);
            }

            Assert.True(true); // The test is successful if it does not throw
        }

        [Fact]
        public void ExchangeDeclarePassive_throws_if_exchange_does_not_exist()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string exchangeName = "someExchange";
                Assert.Throws<OperationInterruptedException>(() => model.ExchangeDeclarePassive(exchange: exchangeName));
            }
        }

        [Fact]
        public void ExchangeDeclareNoWait_creates_exchange()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string exchangeName = "someExchange";
                const string exchangeType = "someType";
                const bool isDurable = true;
                const bool isAutoDelete = false;
                var arguments = new Dictionary<string, object>();

                model.ExchangeDeclareNoWait(exchange: exchangeName, type: exchangeType, durable: isDurable, autoDelete: isAutoDelete, arguments: arguments);
                Assert.Single(server.Exchanges);

                var exchange = server.Exchanges.First();
                AssertEx.AssertExchangeDetails(exchange, exchangeName, isAutoDelete, arguments, isDurable, exchangeType);
            }
        }

        [Fact]
        public void ExchangeDelete_with_only_name_argument_removes_the_exchange_if_it_exists()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string exchangeName = "someExchange";
                model.ExchangeDeclare(exchangeName, "someType");
                model.ExchangeDelete(exchange: exchangeName);
                Assert.Empty(server.Exchanges);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExchangeDelete_removes_the_exchange_if_it_exists(bool ifUnused)
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string exchangeName = "someExchange";
                model.ExchangeDeclare(exchangeName, "someType");
                model.ExchangeDelete(exchange: exchangeName, ifUnused: ifUnused);
                Assert.Empty(server.Exchanges);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExchangeDeleteNoWait_removes_the_exchange_if_it_exists(bool ifUnused)
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string exchangeName = "someExchange";
                model.ExchangeDeclare(exchangeName, "someType");
                model.ExchangeDeleteNoWait(exchange: exchangeName, ifUnused: ifUnused);
                Assert.Empty(server.Exchanges);
            }
        }

        [Fact]
        public void ExchangeDelete_does_nothing_if_exchange_does_not_exist()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string exchangeName = "someExchange";
                model.ExchangeDeclare(exchangeName, "someType");
                model.ExchangeDelete(exchange: "someOtherExchange");
                Assert.Single(server.Exchanges);
            }
        }

        [Fact]
        public void ExchangeBind_binds_an_exchange_to_a_queue()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string queueName = "someQueue";
                const string exchangeName = "someExchange";
                const string routingKey = "someRoutingKey";
                var arguments = new Dictionary<string, object>();

                model.ExchangeDeclare(exchangeName, "direct");
                model.QueueDeclare(queueName);
                model.ExchangeBind(queueName, exchangeName, routingKey, arguments);
                AssertEx.AssertBinding(server, exchangeName, routingKey, queueName);
            }
        }

        [Fact]
        public void ExchangeBindNoWait_binds_an_exchange_to_a_queue()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string queueName = "someQueue";
                const string exchangeName = "someExchange";
                const string routingKey = "someRoutingKey";
                var arguments = new Dictionary<string, object>();

                model.ExchangeDeclare(exchangeName, "direct");
                model.QueueDeclare(queueName);
                model.ExchangeBindNoWait(queueName, exchangeName, routingKey, arguments);
                AssertEx.AssertBinding(server, exchangeName, routingKey, queueName);
            }
        }

        [Fact]
        public void ExchangeUnbind_removes_binding()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string queueName = "someQueue";
                const string exchangeName = "someExchange";
                const string routingKey = "someRoutingKey";
                var arguments = new Dictionary<string, object>();

                model.ExchangeDeclare(exchangeName, "direct");
                model.QueueDeclare(queueName);
                model.ExchangeBind(exchangeName, queueName, routingKey, arguments);
                model.ExchangeUnbind(queueName, exchangeName, routingKey, arguments);

                Assert.True(server.Exchanges[exchangeName].Bindings.IsEmpty);
                Assert.True(server.Queues[queueName].Bindings.IsEmpty);
            }
        }

        [Fact]
        public void ExchangeUnbindNoWait_removes_binding()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string queueName = "someQueue";
                const string exchangeName = "someExchange";
                const string routingKey = "someRoutingKey";
                var arguments = new Dictionary<string, object>();

                model.ExchangeDeclare(exchangeName, "direct");
                model.QueueDeclare(queueName);
                model.ExchangeBind(exchangeName, queueName, routingKey, arguments);
                model.ExchangeUnbindNoWait(queueName, exchangeName, routingKey, arguments);

                Assert.True(server.Exchanges[exchangeName].Bindings.IsEmpty);
                Assert.True(server.Queues[queueName].Bindings.IsEmpty);
            }
        }
    }
}
