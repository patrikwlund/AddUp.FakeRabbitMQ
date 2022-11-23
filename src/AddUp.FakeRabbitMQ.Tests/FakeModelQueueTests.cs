using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace AddUp.RabbitMQ.Fakes
{
    [ExcludeFromCodeCoverage]
    public class FakeModelQueueTests
    {
        [Fact]
        public void QueueBind_binds_an_exchange_to_a_queue()
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
                model.QueueBind(queueName, exchangeName, routingKey, arguments);
                
                AssertEx.AssertBinding(server, exchangeName, routingKey, queueName);
            }
        }

        [Fact]
        public void QueueBindNoWait_binds_an_exchange_to_a_queue()
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
                model.QueueBindNoWait(queueName, exchangeName, routingKey, arguments);

                AssertEx.AssertBinding(server, exchangeName, routingKey, queueName);
            }
        }

        [Fact]
        public void QueueUnbind_removes_binding()
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
                model.QueueUnbind(queueName, exchangeName, routingKey, arguments);
                
                Assert.True(server.Exchanges[exchangeName].Bindings.IsEmpty);
                Assert.Single(server.Queues[queueName].Bindings);
            }
        }

        [Fact]
        public void QueueDeclare_without_arguments_creates_a_queue()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.QueueDeclare();
                Assert.Single(server.Queues);
            }
        }

        [Fact]
        public void QueueDeclarePassive_does_not_throw_if_queue_exists()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string queueName = "myQueue";
                model.QueueDeclare(queueName);
                model.QueueDeclarePassive(queueName);
            }

            Assert.True(true); // The test is successful if it does not throw
        }

        [Fact]
        public void QueueDeclarePassive_throws_if_queue_does_not_exist()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string queueName = "myQueue";
                Assert.Throws<OperationInterruptedException>(() => model.QueueDeclarePassive(queueName));
            }
        }

        [Fact]
        public void QueueDeclare_creates_a_queue()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string queueName = "someQueue";
                const bool isDurable = true;
                const bool isExclusive = true;
                const bool isAutoDelete = false;
                var arguments = new Dictionary<string, object>();

                model.QueueDeclare(queue: queueName, durable: isDurable, exclusive: isExclusive, autoDelete: isAutoDelete, arguments: arguments);
                Assert.Single(server.Queues);

                var queue = server.Queues.First();
                AssertEx.AssertQueueDetails(queue, queueName, isAutoDelete, arguments, isDurable, isExclusive);
            }
        }

        [Fact]
        public void QueueDeclareoWait_creates_a_queue()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string queueName = "someQueue";
                const bool isDurable = true;
                const bool isExclusive = true;
                const bool isAutoDelete = false;
                var arguments = new Dictionary<string, object>();

                model.QueueDeclareNoWait(queue: queueName, durable: isDurable, exclusive: isExclusive, autoDelete: isAutoDelete, arguments: arguments);
                Assert.Single(server.Queues);

                var queue = server.Queues.First();
                AssertEx.AssertQueueDetails(queue, queueName, isAutoDelete, arguments, isDurable, isExclusive);
            }
        }

        [Fact]
        public void QueueDelete_with_onlyèname_argument_deletes_the_queue()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                const string queueName = "someName";
                model.QueueDeclare(queueName, true, true, true, null);
                model.QueueDelete(queueName);
                Assert.True(server.Queues.IsEmpty);
            }
        }

        [Fact]
        public void QueueDelete_with_arguments_deletes_the_queue()
        {
            var node = new RabbitServer();
            using (var model = new FakeModel(node))
            {
                const string queueName = "someName";
                model.QueueDeclare(queueName, true, true, true, null);
                model.QueueDelete(queueName, true, true);
                Assert.True(node.Queues.IsEmpty);
            }
        }

        [Fact]
        public void QueueDeleteNoWait_with_arguments_deletes_the_queue()
        {
            var node = new RabbitServer();
            using (var model = new FakeModel(node))
            {
                const string queueName = "someName";
                model.QueueDeclare(queueName, true, true, true, null);
                model.QueueDeleteNoWait(queueName, true, true);
                Assert.True(node.Queues.IsEmpty);
            }
        }

        [Fact]
        public void QueueDelete_does_nothing_if_queue_does_not_exist()
        {
            var node = new RabbitServer();
            using (var model = new FakeModel(node))
            {
                model.QueueDelete("someQueue");
                Assert.True(node.Queues.IsEmpty);
            }
        }

        [Fact]
        public void QueuePurge_removes_all_messages_from_specified_queue()
        {
            var node = new RabbitServer();
            using (var model = new FakeModel(node))
            {
                model.QueueDeclare("my_other_queue");
                node.Queues["my_other_queue"].Messages.Enqueue(new RabbitMessage());
                node.Queues["my_other_queue"].Messages.Enqueue(new RabbitMessage());

                model.QueueDeclare("my_queue");
                node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage());
                node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage());
                node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage());
                node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage());

                var count = model.QueuePurge("my_queue");
                Assert.Equal(4u, count);

                Assert.True(node.Queues["my_queue"].Messages.IsEmpty);
                Assert.False(node.Queues["my_other_queue"].Messages.IsEmpty);
            }
        }

        [Fact]
        public void QueuePurge_returns_0_if_queue_does_not_exist()
        {
            var node = new RabbitServer();
            using (var model = new FakeModel(node))
            {
                model.QueueDeclare("my_queue");
                node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage());
                node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage());
                node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage());
                node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage());

                var count = model.QueuePurge("my_other_queue");
                Assert.Equal(0u, count);

                Assert.False(node.Queues["my_queue"].Messages.IsEmpty);
            }
        }
    }
}
