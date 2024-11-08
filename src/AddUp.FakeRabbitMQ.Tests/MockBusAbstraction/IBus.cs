using System;
using System.Diagnostics.CodeAnalysis;
using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes.MockBusAbstraction;

// This whole Bus abstraction is something we use in our projects.
// It is reproduced here so as to more easily unit test behaviors
// of FakeConnection and friends.

internal interface IBus : IDisposable
{
    bool IsConnected { get; }

    IBus Connect(bool forceReconnection);
    void Disconnect();
    IChannel CreateChannel();
}

[ExcludeFromCodeCoverage]
internal static class BusExtensions
{
    public static IBus Connect(this IBus bus) => bus.Connect(false);
}
