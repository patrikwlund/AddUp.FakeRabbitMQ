using System.Diagnostics.CodeAnalysis;
using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes.MockBusAbstraction;

// A 'bus' stores both a connection and a channel.
[ExcludeFromCodeCoverage]
internal sealed class Bus : IBus
{
    private bool disposed;

    public Bus() : this((string)null) { }
    public Bus(string hostName) : this(hostName, null, null) { }
    public Bus(string hostName, string userName, string password) : this(new ConnectionFactory
    {
        HostName = hostName ?? DefaultHostName,
        UserName = userName ?? DefaultUserName,
        Password = password ?? DefaultPassword
    })
    { }

    public Bus(IConnectionFactory factory) =>
        ConnectionFactory = factory ?? throw new ArgumentNullException(nameof(factory));

    public static string DefaultHostName => "localhost";
    public static string DefaultUserName => "guest";
    public static string DefaultPassword => "guest";

    public bool IsConnected => Connection != null;

    private IConnectionFactory ConnectionFactory { get; }

    private IConnection Connection { get; set; }

    public IBus Connect(bool forceReconnection)
    {
        if (Connection != null && !forceReconnection)
            return this;

        Disconnect();
        Connection = ConnectionFactory.CreateConnectionAsync("ncore").GetAwaiter().GetResult();

        return this; // Allows chaining of instanciation and connection
    }

    public void Disconnect()
    {
        if (Connection == null)
            return;

        Connection.CloseAsync();
        Connection.Dispose();

        Connection = null;
    }

    public IChannel CreateChannel()
    {
        if (disposed) throw new ObjectDisposedException(nameof(Bus));
        if (Connection == null) throw new InvalidOperationException("You must first connect to the bus");
        return Connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        if (disposed) return;
        Disconnect();
        disposed = true;
    }
}
