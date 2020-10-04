using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace AddUp.RabbitMQ.Fakes
{
    [ExcludeFromCodeCoverage]
    public class FakeConnectionFactoryTests
    {
        [Fact]
        public void CreateConnection_ConnectionNotSupplied_ReturnsFakeConnection()
        {
            // Arrange
            var factory = new FakeConnectionFactory();

            // Act
            var result = factory.CreateConnection();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<FakeConnection>(result);
            Assert.Same(factory.UnderlyingConnection, result);
        }

        [Fact]
        public void WithConnection_WhenSet_SetsTheUnderlyingConnection()
        {
            // Arrange
            var factory = new FakeConnectionFactory();

            var connection = new FakeConnection(new RabbitServer());

            // Act
            factory.WithConnection(connection);

            // Assert
            Assert.Same(connection, factory.Connection);
        }

        [Fact]
        public void UnderlyingConnection_NoConnection_ReturnsNull()
        {
            // Arrange
            var factory = new FakeConnectionFactory();

            // Act
            var result = factory.UnderlyingConnection;

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void UnderlyingConnection_WithConnection_ReturnsConnection()
        {
            // Arrange
            var factory = new FakeConnectionFactory();
            var connection = new FakeConnection(new RabbitServer());
            factory.WithConnection(connection);

            // Act
            var result = factory.UnderlyingConnection;

            // Assert
            Assert.Same(connection, result);
        }

        [Fact]
        public void UnderlyingConnection_WithoutConnection_ReturnsEmptyList()
        {
            // Arrange
            var factory = new FakeConnectionFactory();

            // Act
            var result = factory.UnderlyingModel;

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void WithRabbitServer_SetsServer()
        {
            // Arrange
            var factory = new FakeConnectionFactory();
            var otherServer = new RabbitServer();
            factory.WithRabbitServer(otherServer);

            // Act
            var result = factory.Server;

            // Assert
            Assert.Same(otherServer, result);
        }
    }
}