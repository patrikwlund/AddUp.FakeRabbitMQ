using RabbitMQ.Client;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace AddUp.RabbitMQ.Fakes.Repros;

[ExcludeFromCodeCoverage]
public class CloseConnectionTests
{
    // This ensures issue #27 is fixed
    [Fact]
    public async Task Closing_a_connection_after_disconnection_then_reconnection_should_not_throw()
    {
        var factory = new FakeConnectionFactory();
        var connection = await factory.CreateConnectionAsync();
        await connection.AbortAsync();

        connection = await factory.CreateConnectionAsync();
        await connection.AbortAsync();

        Assert.True(true); // If we didn't throw, we reached this point.
    }
}
