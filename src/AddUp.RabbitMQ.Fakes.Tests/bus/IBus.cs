using System;
using System.Diagnostics.CodeAnalysis;
using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes
{
    // This whole Bus abstraction is something we use in our projects.
    // It is reproduced here so as to more easily unit test behaviors
    // of FakeConnection and friends.

    public interface IBus : IDisposable
    {
        bool IsConnected { get; }

        IBus Connect(bool forceReconnection);
        void Disconnect();
        IModel CreateChannel();
    }

    [ExcludeFromCodeCoverage]
    public static class BusExtensions
    {
        public static IBus Connect(this IBus bus) => bus.Connect(false);
    }
}
