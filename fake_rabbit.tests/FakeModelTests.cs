using System.Collections;
using System.Collections.Generic;
using System.Linq;
using fake_rabbit.models;
using NUnit.Framework;

namespace fake_rabbit.tests
{
    [TestFixture]
    public class FakeModelTests
    {
        [Test]
        public void CreateBasicProperties_ReturnsBasicProperties()
        {
            // Arrange
            var model = new FakeModel();

            // Act
            var result = model.CreateBasicProperties();

            // Assert
            Assert.That(result,Is.Not.Null);
        }

        [Test]
        public void CreateFileProperties_ReturnsFileProperties()
        {
            // Arrange
            var model = new FakeModel();

            // Act
            var result = model.CreateFileProperties();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void CreateStreamProperties_ReturnsStreamProperties()
        {
            // Arrange
            var model = new FakeModel();

            // Act
            var result = model.CreateStreamProperties();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ChannelFlow_SetsIfTheChannelIsActive(bool value)
        {
            // Arrange
            var model = new FakeModel();

            // Act
            model.ChannelFlow(value);

            // Assert
            Assert.That(model.IsChannelFlowActive,Is.EqualTo(value));
        }

        [Test]
        public void ExchangeDeclare_AllArguments_CreatesExchange()
        {
            // Arrange
            var model = new FakeModel();

            const string exchangeName = "someExchange";
            const string exchangeType = "someType";
            const bool isDurable = true;
            const bool isAutoDelete = false;
            var arguments = new Hashtable();

            // Act
            model.ExchangeDeclare(exchange:exchangeName,type:exchangeType,durable:isDurable,autoDelete:isAutoDelete,arguments:arguments);
        
            // Assert
            Assert.That(model.Exchanges,Has.Count.EqualTo(1));

            var exchange = model.Exchanges.First();
            AssertExchangeDetails(exchange, exchangeName, isAutoDelete, arguments, isDurable, exchangeType);
        }

        [Test]
        public void ExchangeDeclare_WithNameTypeAndDurable_CreatesExchange()
        {
            // Arrange
            var model = new FakeModel();

            const string exchangeName = "someExchange";
            const string exchangeType = "someType";
            const bool isDurable = true;

            // Act
            model.ExchangeDeclare(exchange: exchangeName, type: exchangeType, durable: isDurable);

            // Assert
            Assert.That(model.Exchanges, Has.Count.EqualTo(1));

            var exchange = model.Exchanges.First();
            AssertExchangeDetails(exchange, exchangeName, false, null, isDurable, exchangeType);
        }

        [Test]
        public void ExchangeDeclare_WithNameType_CreatesExchange()
        {
            // Arrange
            var model = new FakeModel();

            const string exchangeName = "someExchange";
            const string exchangeType = "someType";

            // Act
            model.ExchangeDeclare(exchange: exchangeName, type: exchangeType);

            // Assert
            Assert.That(model.Exchanges, Has.Count.EqualTo(1));

            var exchange = model.Exchanges.First();
            AssertExchangeDetails(exchange, exchangeName, false, null, false, exchangeType);
        }

        [Test]
        public void ExchangeDeclarePassive_WithName_CreatesExchange()
        {
            // Arrange
            var model = new FakeModel();

            const string exchangeName = "someExchange";

            // Act
            model.ExchangeDeclarePassive(exchange: exchangeName);

            // Assert
            Assert.That(model.Exchanges, Has.Count.EqualTo(1));

            var exchange = model.Exchanges.First();
            AssertExchangeDetails(exchange, exchangeName, false, null, false, null);
        }

        [Test]
        public void ExchangeDeclareNoWait_CreatesExchange()
        {
            // Arrange
            var model = new FakeModel();

            const string exchangeName = "someExchange";
            const string exchangeType = "someType";
            const bool isDurable = true;
            const bool isAutoDelete = false;
            var arguments = new Hashtable();

            // Act
            model.ExchangeDeclareNoWait(exchange: exchangeName, type: exchangeType, durable: isDurable, autoDelete: isAutoDelete, arguments: arguments);

            // Assert
            Assert.That(model.Exchanges, Has.Count.EqualTo(1));

            var exchange = model.Exchanges.First();
            AssertExchangeDetails(exchange, exchangeName, isAutoDelete, arguments, isDurable, exchangeType);
        }

        private static void AssertExchangeDetails(KeyValuePair<string, Exchange> exchange, string exchangeName, bool isAutoDelete,Hashtable arguments, bool isDurable, string exchangeType)
        {
            Assert.That(exchange.Key, Is.EqualTo(exchangeName));
            Assert.That(exchange.Value.AutoDelete, Is.EqualTo(isAutoDelete));
            Assert.That(exchange.Value.Arguments, Is.EqualTo(arguments));
            Assert.That(exchange.Value.IsDurable, Is.EqualTo(isDurable));
            Assert.That(exchange.Value.Name, Is.EqualTo(exchangeName));
            Assert.That(exchange.Value.Type, Is.EqualTo(exchangeType));
        }

        [Test]
        public void ExchangeDelete_NameOnlyExchangeExists_RemovesTheExchange()
        {
            // Arrange
            var model = new FakeModel();

            const string exchangeName = "someExchange";
            model.ExchangeDeclare(exchangeName,"someType");

            // Act
            model.ExchangeDelete(exchange: exchangeName);

            // Assert
            Assert.That(model.Exchanges, Has.Count.EqualTo(0));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ExchangeDelete_ExchangeExists_RemovesTheExchange(bool ifUnused)
        {
            // Arrange
            var model = new FakeModel();

            const string exchangeName = "someExchange";
            model.ExchangeDeclare(exchangeName, "someType");

            // Act
            model.ExchangeDelete(exchange: exchangeName,ifUnused:ifUnused);

            // Assert
            Assert.That(model.Exchanges, Has.Count.EqualTo(0));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ExchangeDeleteNoWait_ExchangeExists_RemovesTheExchange(bool ifUnused)
        {
            // Arrange
            var model = new FakeModel();

            const string exchangeName = "someExchange";
            model.ExchangeDeclare(exchangeName, "someType");

            // Act
            model.ExchangeDeleteNoWait(exchange: exchangeName, ifUnused: ifUnused);

            // Assert
            Assert.That(model.Exchanges, Has.Count.EqualTo(0));
        }

        [Test]
        public void ExchangeDelete_ExchangeDoesNotExists_DoesNothing()
        {
            // Arrange
            var model = new FakeModel();

            const string exchangeName = "someExchange";
            model.ExchangeDeclare(exchangeName, "someType");

            // Act
            model.ExchangeDelete(exchange: "someOtherExchange");

            // Assert
            Assert.That(model.Exchanges, Has.Count.EqualTo(1));

        }

        [Test]
        public void ExchangeBind_BindsAnExchangeToAQueue()
        {
            // Arrange
            var model = new FakeModel();

            const string queueName = "someQueue";
            const string exchangeName = "someExchange";
            const string routingKey = "someRoutingKey";
            var arguments = new Hashtable();

            model.ExchangeDeclare(exchangeName,"direct");
            model.QueueDeclarePassive(queueName);

            // Act
            model.ExchangeBind(queueName, exchangeName, routingKey, arguments);

            // Assert
            AssertBinding(model, exchangeName, routingKey, queueName);
        }

        [Test]
        public void QueueBind_BindsAnExchangeToAQueue()
        {
            // Arrange
            var model = new FakeModel();

            const string queueName = "someQueue";
            const string exchangeName = "someExchange";
            const string routingKey = "someRoutingKey";
            var arguments = new Hashtable();

            model.ExchangeDeclare(exchangeName, "direct");
            model.QueueDeclarePassive(queueName);

            // Act
            model.QueueBind(queueName, exchangeName, routingKey, arguments);

            // Assert
            AssertBinding(model, exchangeName, routingKey, queueName);
        }

        private static void AssertBinding(FakeModel model, string exchangeName, string routingKey, string queueName)
        {
            Assert.That(model.Exchanges[exchangeName].Bindings, Has.Count.EqualTo(1));
            Assert.That(model.Exchanges[exchangeName].Bindings.First().Value.RoutingKey, Is.EqualTo(routingKey));
            Assert.That(model.Exchanges[exchangeName].Bindings.First().Value.Queue.Name, Is.EqualTo(queueName));
        }

        [Test]
        public void ExchangeUnbind_RemovesBinding()
        {
            // Arrange
            var model = new FakeModel();

            const string queueName = "someQueue";
            const string exchangeName = "someExchange";
            const string routingKey = "someRoutingKey";
            var arguments = new Hashtable();

            model.ExchangeDeclare(exchangeName, "direct");
            model.QueueDeclarePassive(queueName);
            model.ExchangeBind(exchangeName,queueName,routingKey,arguments);

            // Act
            model.ExchangeUnbind(queueName, exchangeName, routingKey, arguments);

            // Assert
            Assert.That(model.Exchanges[exchangeName].Bindings, Is.Empty);
            Assert.That(model.Queues[queueName].Bindings, Is.Empty);
        }

        [Test]
        public void QueueUnbind_RemovesBinding()
        {
            // Arrange
            var model = new FakeModel();

            const string queueName = "someQueue";
            const string exchangeName = "someExchange";
            const string routingKey = "someRoutingKey";
            var arguments = new Hashtable();

            model.ExchangeDeclare(exchangeName, "direct");
            model.QueueDeclarePassive(queueName);
            model.ExchangeBind(exchangeName, queueName, routingKey, arguments);

            // Act
            model.QueueUnbind(queueName, exchangeName, routingKey, arguments);

            // Assert
            Assert.That(model.Exchanges[exchangeName].Bindings, Is.Empty);
            Assert.That(model.Queues[queueName].Bindings, Is.Empty);
        }
    }
}