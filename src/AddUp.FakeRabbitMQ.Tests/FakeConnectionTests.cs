using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using RabbitMQ.Client;
using Xunit;

namespace AddUp.RabbitMQ.Fakes;

[ExcludeFromCodeCoverage]
public class FakeConnectionTests
{
    [Fact]
    public async Task CreateChannel_creates_a_new_channel()
    {
        var connection = new FakeConnection(new RabbitServer());
        var channel = await connection.CreateChannelAsync();

        Assert.Single(connection.GetModelsForUnitTests());
        connection.GetModelsForUnitTests().Should().BeEquivalentTo(new[] { channel });
    }

    [Fact]
    public async Task CreateChannel_called_multiple_times_creates_models()
    {
        var connection = new FakeConnection(new RabbitServer());

        var channel1 = await connection.CreateChannelAsync();
        var channel2 = await connection.CreateChannelAsync();

        Assert.Equal(2, connection.GetModelsForUnitTests().Count);
        connection.GetModelsForUnitTests().Should().BeEquivalentTo([channel1, channel2]);
    }

    [Fact]
    public async Task Close_without_arguments_closes_the_connection()
    {
        var connection = new FakeConnection(new RabbitServer());
        await connection.CloseAsync();

        Assert.False(connection.IsOpen);
        Assert.NotNull(connection.CloseReason);
    }

    [Fact]
    public async Task Close_with_timeout_argument_closes_the_connection()
    {
        var connection = new FakeConnection(new RabbitServer());
        await connection.CloseAsync(timeout: TimeSpan.FromSeconds(2.0));

        Assert.False(connection.IsOpen);
        Assert.NotNull(connection.CloseReason);
    }

    [Fact]
    public async Task Close_with_reason_argument_closes_the_connection()
    {
        var connection = new FakeConnection(new RabbitServer());
        await connection.CloseAsync(reasonCode: 3, reasonText: "foo");

        Assert.False(connection.IsOpen);
        Assert.Equal(3, connection.CloseReason.ReplyCode);
        Assert.Equal("foo", connection.CloseReason.ReplyText);
    }

    [Fact]
    public async Task Close_with_all_arguments_closes_the_connection()
    {
        var connection = new FakeConnection(new RabbitServer());
        await connection.CloseAsync(reasonCode: 3, reasonText: "foo", timeout: TimeSpan.FromSeconds(4.0));

        Assert.False(connection.IsOpen);
        Assert.Equal(3, connection.CloseReason.ReplyCode);
        Assert.Equal("foo", connection.CloseReason.ReplyText);
    }

    [Fact]
    public async Task Close_closes_all_models()
    {
        var connection = new FakeConnection(new RabbitServer());
        _ = await connection.CreateChannelAsync();

        await connection.CloseAsync();

        Assert.True(connection.GetModelsForUnitTests().TrueForAll(m => !m.IsOpen));
        Assert.True(connection.GetModelsForUnitTests().TrueForAll(m => m.IsClosed));
    }

    [Fact]
    public async Task Abort_without_arguments_aborts_the_connection()
    {
        var connection = new FakeConnection(new RabbitServer());
        await connection.AbortAsync();

        Assert.False(connection.IsOpen);
        Assert.NotNull(connection.CloseReason);
    }

    [Fact]
    public async Task Abort_with_timeout_argument_aborts_the_connection()
    {
        var connection = new FakeConnection(new RabbitServer());
        await connection.AbortAsync(timeout: TimeSpan.FromSeconds(2.0));

        Assert.False(connection.IsOpen);
        Assert.NotNull(connection.CloseReason);
    }

    [Fact]
    public async Task Abort_with_reason_argument_aborts_the_connection()
    {
        var connection = new FakeConnection(new RabbitServer());

        await connection.AbortAsync(reasonCode: 3, reasonText: "foo");

        Assert.False(connection.IsOpen);
        Assert.Equal(3, connection.CloseReason.ReplyCode);
        Assert.Equal("foo", connection.CloseReason.ReplyText);
    }

    [Fact]
    public async Task Abort_with_all_arguments_aborts_the_connection()
    {
        var connection = new FakeConnection(new RabbitServer());
        await connection.AbortAsync(reasonCode: 3, reasonText: "foo", timeout: TimeSpan.FromSeconds(4.0));

        Assert.False(connection.IsOpen);
        Assert.Equal(3, connection.CloseReason.ReplyCode);
        Assert.Equal("foo", connection.CloseReason.ReplyText);
    }

    [Fact]
    public async Task Abort_aborts_all_channels()
    {
        var connection = new FakeConnection(new RabbitServer());
        await connection.CreateChannelAsync();

        await connection.AbortAsync();

        Assert.True(connection.GetModelsForUnitTests().TrueForAll(m => !m.IsOpen));
        Assert.True(connection.GetModelsForUnitTests().TrueForAll(m => m.IsClosed));
    }
}
