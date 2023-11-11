using System.Diagnostics.CodeAnalysis;
using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes.MockBusAbstraction;

[ExcludeFromCodeCoverage]
internal sealed class MockBus : IBus
{
    private readonly Bus bus;

    public MockBus() => bus = new Bus(new FakeConnectionFactory());

    public bool IsConnected => bus.IsConnected;

    public IBus Connect(bool forceReconnection) => bus.Connect(forceReconnection);
    public IModel CreateChannel() => bus.CreateChannel();
    public void Disconnect() => bus.Disconnect();
    public void Dispose() => bus.Dispose();
}    
