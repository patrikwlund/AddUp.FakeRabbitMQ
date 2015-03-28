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
    }
}