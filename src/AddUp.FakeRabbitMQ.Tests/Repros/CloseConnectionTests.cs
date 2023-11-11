using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace AddUp.RabbitMQ.Fakes.Repros;

[ExcludeFromCodeCoverage]
public class CloseConnectionTests
{
    // This ensures issue #27 is fixed
    [Fact]
    public void Closing_a_connection_after_disconnection_then_reconnection_should_not_throw()
    {
        var factory = new FakeConnectionFactory();
        var connection = factory.CreateConnection();
        connection.Close();

        connection = factory.CreateConnection();
        connection.Close();

        Assert.True(true); // If we didn't throw, we reached this point.
    }
}
