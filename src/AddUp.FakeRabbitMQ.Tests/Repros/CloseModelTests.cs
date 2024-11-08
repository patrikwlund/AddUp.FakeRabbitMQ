using RabbitMQ.Client;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace AddUp.RabbitMQ.Fakes.Repros;

[ExcludeFromCodeCoverage]
public class CloseModelTests
{
    // This ensures issue #25 is fixed
    [Fact]
    public async Task Closing_a_model_after_disconnection_should_not_throw()
    {
        var factory = new FakeConnectionFactory();
        var connection = await factory.CreateConnectionAsync();

        var model = await connection.CreateChannelAsync();
        await connection.AbortAsync();
        await model.CloseAsync();

        Assert.True(true); // If we didn't throw, we reached this point.
    }
}
