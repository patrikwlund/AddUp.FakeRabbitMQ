using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace AddUp.RabbitMQ.Fakes
{
    [ExcludeFromCodeCoverage]
    public class FakeConnectionTests
    {
        [Fact]
        public void CreateModel_creates_a_new_model()
        {
            var connection = new FakeConnection(new RabbitServer());
            var model = connection.CreateModel();

            Assert.Single(connection.GetModelsForUnitTests());
            connection.GetModelsForUnitTests().Should().BeEquivalentTo(new[] { model });
        }

        [Fact]
        public void CreateModel_called_multiple_times_creates_models()
        {
            var connection = new FakeConnection(new RabbitServer());

            var model1 = connection.CreateModel();
            var model2 = connection.CreateModel();

            Assert.Equal(2, connection.GetModelsForUnitTests().Count);
            connection.GetModelsForUnitTests().Should().BeEquivalentTo(new[] { model1, model2 });
        }

        [Fact]
        public void Close_without_arguments_closes_the_connection()
        {
            var connection = new FakeConnection(new RabbitServer());
            connection.Close();

            Assert.False(connection.IsOpen);
            Assert.NotNull(connection.CloseReason);
        }

        [Fact]
        public void Close_with_timeout_argument_closes_the_connection()
        {
            var connection = new FakeConnection(new RabbitServer());
            connection.Close(timeout: TimeSpan.FromSeconds(2.0));

            Assert.False(connection.IsOpen);
            Assert.NotNull(connection.CloseReason);
        }

        [Fact]
        public void Close_with_reason_argument_closes_the_connection()
        {
            var connection = new FakeConnection(new RabbitServer());
            connection.Close(reasonCode: 3, reasonText: "foo");

            Assert.False(connection.IsOpen);
            Assert.Equal(3, connection.CloseReason.ReplyCode);
            Assert.Equal("foo", connection.CloseReason.ReplyText);
        }

        [Fact]
        public void Close_with_all_arguments_closes_the_connection()
        {
            var connection = new FakeConnection(new RabbitServer());
            connection.Close(reasonCode: 3, reasonText: "foo", timeout: TimeSpan.FromSeconds(4.0));

            Assert.False(connection.IsOpen);
            Assert.Equal(3, connection.CloseReason.ReplyCode);
            Assert.Equal("foo", connection.CloseReason.ReplyText);
        }

        [Fact]
        public void Close_closes_all_models()
        {
            var connection = new FakeConnection(new RabbitServer());
            _ = connection.CreateModel();

            connection.Close();

            Assert.True(connection.GetModelsForUnitTests().All(m => !m.IsOpen));
            Assert.True(connection.GetModelsForUnitTests().All(m => m.IsClosed));
        }

        [Fact]
        public void Abort_without_arguments_aborts_the_connection()
        {
            var connection = new FakeConnection(new RabbitServer());
            connection.Abort();

            Assert.False(connection.IsOpen);
            Assert.NotNull(connection.CloseReason);
        }

        [Fact]
        public void Abort_with_timeout_argument_aborts_the_connection()
        {
            var connection = new FakeConnection(new RabbitServer());
            connection.Abort(timeout: TimeSpan.FromSeconds(2.0));

            Assert.False(connection.IsOpen);
            Assert.NotNull(connection.CloseReason);
        }

        [Fact]
        public void Abort_with_reason_argument_aborts_the_connection()
        {
            var connection = new FakeConnection(new RabbitServer());

            connection.Abort(reasonCode: 3, reasonText: "foo");

            Assert.False(connection.IsOpen);
            Assert.Equal(3, connection.CloseReason.ReplyCode);
            Assert.Equal("foo", connection.CloseReason.ReplyText);
        }

        [Fact]
        public void Abort_with_all_arguments_aborts_the_connection()
        {
            var connection = new FakeConnection(new RabbitServer());
            connection.Abort(reasonCode: 3, reasonText: "foo", timeout: TimeSpan.FromSeconds(4.0));

            Assert.False(connection.IsOpen);
            Assert.Equal(3, connection.CloseReason.ReplyCode);
            Assert.Equal("foo", connection.CloseReason.ReplyText);
        }

        [Fact]
        public void Abort_aborts_all_models()
        {
            var connection = new FakeConnection(new RabbitServer());
            connection.CreateModel();

            connection.Abort();

            Assert.True(connection.GetModelsForUnitTests().All(m => !m.IsOpen));
            Assert.True(connection.GetModelsForUnitTests().All(m => m.IsClosed));
        }
    }
}
