using NUnit.Framework;
using RabbitMQ.Fakes.models;

namespace RabbitMQ.Fakes.Tests
{
    [TestFixture]
    public class FakeConnectionFactoryTests
    {
        [Test]
        public void CreateConnection_ConnectionNotSupplied_ReturnsFakeConnection()
        {
            // Arrange
            var factory = new FakeConnectionFactory();

            // Act
            var result = factory.CreateConnection();

            // Assert
            Assert.That(result,Is.Not.Null);
            Assert.That(result,Is.InstanceOf<FakeConnection>());
            Assert.That(factory.UnderlyingConnection, Is.SameAs(result));
        }

        [Test]
        public void WithConnection_WhenSet_SetsTheUnderlyingConnection()
        {
            // Arrange
            var factory = new FakeConnectionFactory();

            var connection = new FakeConnection(new RabbitServer());

            // Act
            factory.WithConnection(connection);

            // Assert
            Assert.That(factory.Connection,Is.SameAs(connection));
        }

        [Test]
        public void UnderlyingConnection_NoConnection_ReturnsNull()
        {
            // Arrange
            var factory = new FakeConnectionFactory();

            // Act
            var result = factory.UnderlyingConnection;

            // Assert
            Assert.That(result,Is.Null);
        }

        [Test]
        public void UnderlyingConnection_WithConnection_ReturnsConnection()
        {
            // Arrange
            var factory = new FakeConnectionFactory();
            var connection = new FakeConnection(new RabbitServer());
            factory.WithConnection(connection);

            // Act
            var result = factory.UnderlyingConnection;

            // Assert
            Assert.That(result, Is.SameAs(connection));
        }

        [Test]
        public void UnderlyingConnection_WithoutConnection_ReturnsEmptyList()
        {
            // Arrange
            var factory = new FakeConnectionFactory();

            // Act
            var result = factory.UnderlyingModel;

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void WithRabbitServer_SetsServer()
        {
            // Arrange
            var factory = new FakeConnectionFactory();
            var otherServer = new RabbitServer();
            factory.WithRabbitServer(otherServer);

            // Act
            var result = factory.Server;

            // Assert
            Assert.That(result, Is.SameAs(otherServer));
        }


    }
}