using RabbitMQ.Client;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Xunit;

namespace AddUp.RabbitMQ.Fakes.Repros;

[ExcludeFromCodeCoverage]
public class ChannelWithBasicReturnTests
{
    // This ensures issue #5 is fixed
    [Fact]
    public async Task A_Channel_with_its_BasicReturn_event_bound_should_not_throw()
    {
        var rabbitServer = new RabbitServer();
        var connectionFactory = new FakeConnectionFactory(rabbitServer);

        using var publisherConnection = await connectionFactory.CreateConnectionAsync();
        using var publisherChannel = await publisherConnection.CreateChannelAsync();

        publisherChannel.BasicReturnAsync += (s, e) => Task.CompletedTask;

        const string message = "hello world!";
        var messageBody = Encoding.ASCII.GetBytes(message);
        await publisherChannel.BasicPublishAsync("test_exchange", "foo.bar.baz", false, messageBody);

        Assert.True(true); // If we didn't throw, we reached this point.
    }
}
