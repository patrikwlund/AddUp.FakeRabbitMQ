using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Xunit;

namespace AddUp.RabbitMQ.Fakes;

[ExcludeFromCodeCoverage]
internal static class AssertEx
{
    public static void AssertExchangeDetails(
        KeyValuePair<string, RabbitExchange> exchange, 
        string exchangeName,
        bool isAutoDelete, 
        IDictionary<string, object> arguments,
        bool isDurable,
        string exchangeType)
    {
        Assert.Equal(exchangeName, exchange.Key);
        Assert.Equal(isAutoDelete, exchange.Value.AutoDelete);
        Assert.Equal(arguments, exchange.Value.Arguments);
        Assert.Equal(isDurable, exchange.Value.IsDurable);
        Assert.Equal(exchangeName, exchange.Value.Name);
        Assert.Equal(exchangeType, exchange.Value.Type);
    }

    public static void AssertQueueDetails(
        KeyValuePair<string, RabbitQueue> queue,
        string exchangeName,
        bool isAutoDelete,
        Dictionary<string, object> arguments,
        bool isDurable,
        bool isExclusive)
    {
        Assert.Equal(exchangeName, queue.Key);
        Assert.Equal(isAutoDelete, queue.Value.IsAutoDelete);
        Assert.Equal(arguments, queue.Value.Arguments);
        Assert.Equal(isDurable, queue.Value.IsDurable);
        Assert.Equal(exchangeName, queue.Value.Name);
        Assert.Equal(isExclusive, queue.Value.IsExclusive);
    }

    public static void AssertBinding(RabbitServer server, string exchangeName, string routingKey, string queueName)
    {
        Assert.Single(server.Exchanges[exchangeName].Bindings);
        Assert.Equal(routingKey, server.Exchanges[exchangeName].Bindings.First().Value.RoutingKey);
        Assert.Equal(queueName, server.Exchanges[exchangeName].Bindings.First().Value.Queue.Name);
    }
}
