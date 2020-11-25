using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Xunit;

namespace AddUp.RabbitMQ.Fakes
{
    [ExcludeFromCodeCoverage]
    public class FakeConnectionFactoryTests
    {
        [Fact]
        public void Constructor_throws_when_supplied_a_null_server() =>
            Assert.Throws<ArgumentNullException>(() => new FakeConnectionFactory(null));

        [Fact]
        public void CreateConnection_with_no_host_names_returns_FakeConnection()
        {
            var factory = new FakeConnectionFactory();
            var result = factory.CreateConnection();

            Assert.NotNull(result);
            Assert.IsType<FakeConnection>(result);
        }

        [Fact]
        public void CreateConnection_with_host_names_returns_FakeConnection()
        {
            var factory = new FakeConnectionFactory();
            var result = factory.CreateConnection(new[] { "localhost" }.ToList());

            Assert.NotNull(result);
            Assert.IsType<FakeConnection>(result);
        }

        [Fact]
        public void CreateConnection_with_endpoints_returns_FakeConnection()
        {
            var factory = new FakeConnectionFactory();
            var result = factory.CreateConnection(new[] { new AmqpTcpEndpoint("localhost") }.ToList());

            Assert.NotNull(result);
            Assert.IsType<FakeConnection>(result);
        }

        [Fact]
        public void AuthMechanismFactory_returns_an_intance_of_AuthMechanismFactory()
        {
            var factory = new FakeConnectionFactory();
            var authFactory = factory.AuthMechanismFactory(null);

            Assert.NotNull(authFactory);
        }

        [Fact]
        public void Connection_is_null_when_no_connection_was_provided()
        {
            var factory = new FakeConnectionFactory();
            Assert.Null(factory.Connection);
        }

        [Fact]
        public void Properties_retain_their_values_when_set()
        {
            var factory = new FakeConnectionFactory
            {
                ClientProperties = new Dictionary<string, object> { ["42"] = 42 },
                Password = "p@ssw0rd",
                RequestedChannelMax = 1,
                RequestedFrameMax = 1u,
                RequestedHeartbeat = 1,
                UseBackgroundThreadsForIO = true,
                UserName = "johndoe",
                VirtualHost = "host",
                Uri = new Uri("http://foo.bar.baz/"),
                TaskScheduler = TaskScheduler.Default,
                HandshakeContinuationTimeout = TimeSpan.FromSeconds(1.0),
                ContinuationTimeout = TimeSpan.FromSeconds(1.0)
            };

            Assert.Equal(42, factory.ClientProperties["42"]);
            Assert.Equal("p@ssw0rd", factory.Password);
            Assert.Equal((ushort)1, factory.RequestedChannelMax);
            Assert.Equal(1u, factory.RequestedFrameMax);
            Assert.Equal((ushort)1, factory.RequestedHeartbeat);
            Assert.True(factory.UseBackgroundThreadsForIO);
            Assert.Equal("johndoe", factory.UserName);
            Assert.Equal("host", factory.VirtualHost);
            Assert.Equal("http://foo.bar.baz/", factory.Uri.ToString());
            Assert.NotNull(factory.TaskScheduler);
            Assert.Equal(1, factory.HandshakeContinuationTimeout.TotalSeconds);
            Assert.Equal(1, factory.ContinuationTimeout.TotalSeconds);
        }
    }
}