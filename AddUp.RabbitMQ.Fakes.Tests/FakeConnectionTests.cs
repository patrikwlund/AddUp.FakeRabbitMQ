using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace RabbitMQ.Fakes.Tests
{
    [ExcludeFromCodeCoverage]
    public class FakeConnectionTests
    {
        [Fact]
        public void CreateModel_CreatesANewModel()
        {
            // Arrange
            var connection = new FakeConnection(new RabbitServer());

            // Act
            var result = connection.CreateModel();

            // Assert
            Assert.Single(connection.Models);
            connection.Models.Should().BeEquivalentTo(new[] { result });
        }

        [Fact]
        public void CreateModel_MultipleTimes_CreatesManyModels()
        {
            // Arrange
            var connection = new FakeConnection(new RabbitServer());

            // Act
            var result1 = connection.CreateModel();
            var result2 = connection.CreateModel();

            // Assert
            Assert.Equal(2, connection.Models.Count);
            connection.Models.Should().BeEquivalentTo(new[] { result1, result2 });
        }

        [Fact]
        public void Close_NoArguments_ClosesTheConnection()
        {
            // Arrange
            var connection = new FakeConnection(new RabbitServer());

            // Act
            connection.Close();

            // Assert
            Assert.False(connection.IsOpen);
            Assert.NotNull(connection.CloseReason);
        }

        [Fact]
        public void Close_TimeoutArguments_ClosesTheConnection()
        {
            // Arrange
            var connection = new FakeConnection(new RabbitServer());

            // Act
            connection.Close(timeout: 2);

            // Assert
            Assert.False(connection.IsOpen);
            Assert.NotNull(connection.CloseReason);
        }

        [Fact]
        public void Close_ReasonArguments_ClosesTheConnection()
        {
            // Arrange
            var connection = new FakeConnection(new RabbitServer());

            // Act
            connection.Close(reasonCode: 3, reasonText: "foo");

            // Assert
            Assert.False(connection.IsOpen);
            Assert.Equal(3, connection.CloseReason.ReplyCode);
            Assert.Equal("foo", connection.CloseReason.ReplyText);
        }

        [Fact]
        public void Close_AllArguments_ClosesTheConnection()
        {
            // Arrange
            var connection = new FakeConnection(new RabbitServer());

            // Act
            connection.Close(reasonCode: 3, reasonText: "foo", timeout: 4);

            // Assert
            Assert.False(connection.IsOpen);
            Assert.Equal(3, connection.CloseReason.ReplyCode);
            Assert.Equal("foo", connection.CloseReason.ReplyText);
        }

        [Fact]
        public void Close_ClosesAllModels()
        {
            // Arrange
            var connection = new FakeConnection(new RabbitServer());
            connection.CreateModel();

            // Act
            connection.Close();

            // Assert
            Assert.True(connection.Models.All(m => !m.IsOpen));
            Assert.True(connection.Models.All(m => m.IsClosed));
        }

        [Fact]
        public void Abort_NoArguments_AbortsTheConnection()
        {
            // Arrange
            var connection = new FakeConnection(new RabbitServer());

            // Act
            connection.Abort();

            // Assert
            Assert.False(connection.IsOpen);
            Assert.NotNull(connection.CloseReason);
        }

        [Fact]
        public void Abort_TimeoutArguments_AbortsTheConnection()
        {
            // Arrange
            var connection = new FakeConnection(new RabbitServer());

            // Act
            connection.Abort(timeout: 2);

            // Assert
            Assert.False(connection.IsOpen);
            Assert.NotNull(connection.CloseReason);
        }

        [Fact]
        public void Abort_ReasonArguments_AbortsTheConnection()
        {
            // Arrange
            var connection = new FakeConnection(new RabbitServer());

            // Act
            connection.Abort(reasonCode: 3, reasonText: "foo");

            // Assert
            Assert.False(connection.IsOpen);
            Assert.Equal(3, connection.CloseReason.ReplyCode);
            Assert.Equal("foo", connection.CloseReason.ReplyText);
        }

        [Fact]
        public void Abort_AllArguments_AbortsTheConnection()
        {
            // Arrange
            var connection = new FakeConnection(new RabbitServer());

            // Act
            connection.Abort(reasonCode: 3, reasonText: "foo", timeout: 4);

            // Assert
            Assert.False(connection.IsOpen);
            Assert.Equal(3, connection.CloseReason.ReplyCode);
            Assert.Equal("foo", connection.CloseReason.ReplyText);
        }

        [Fact]
        public void Abort_AbortsAllModels()
        {
            // Arrange
            var connection = new FakeConnection(new RabbitServer());
            connection.CreateModel();

            // Act
            connection.Abort();

            // Assert
            Assert.True(connection.Models.All(m => !m.IsOpen));
            Assert.True(connection.Models.All(m => m.IsClosed));
        }
    }
}