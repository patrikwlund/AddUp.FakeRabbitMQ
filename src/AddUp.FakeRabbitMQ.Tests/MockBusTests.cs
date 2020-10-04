using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace AddUp.RabbitMQ.Fakes
{
    [ExcludeFromCodeCoverage]
    public class MockBusTests
    {
        [Fact]
        public void Creating_a_channel_throws_if_bus_is_not_connected() => Assert.Throws<InvalidOperationException>(() =>
        {
            using var bus = new MockBus();
            using var channel = bus.CreateChannel();
        });

        [Fact]
        public void Creating_a_channel_succeeds_if_bus_is_connected()
        {
            using var bus = new MockBus().Connect();
            using var channel = bus.CreateChannel();

            Assert.True(true);
        }
    }
}
