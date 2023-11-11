using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using RabbitMQ.Client;
using Xunit;

namespace AddUp.RabbitMQ.Fakes;

[ExcludeFromCodeCoverage]
public class FakeBasicPropertiesTests
{
    private static readonly Dictionary<string, object> initialValues;
    private static readonly Dictionary<string, MethodInfo> getters;
    private static readonly Dictionary<string, MethodInfo> setters;
    private static readonly Dictionary<string, MethodInfo> isPresentMethods;
    private static readonly Dictionary<string, MethodInfo> clearMethods;

    static FakeBasicPropertiesTests()
    {
        initialValues = new Dictionary<string, object>
        {
            [nameof(IBasicProperties.AppId)] = null,
            [nameof(IBasicProperties.ClusterId)] = null,
            [nameof(IBasicProperties.ContentEncoding)] = null,
            [nameof(IBasicProperties.ContentType)] = null,
            [nameof(IBasicProperties.CorrelationId)] = null,
            [nameof(IBasicProperties.DeliveryMode)] = (byte)0,
            [nameof(IBasicProperties.Expiration)] = null,
            [nameof(IBasicProperties.Headers)] = null, // IDictionary<string, object>
            [nameof(IBasicProperties.MessageId)] = null,
            [nameof(IBasicProperties.Persistent)] = false,
            [nameof(IBasicProperties.Priority)] = (byte)0,
            [nameof(IBasicProperties.ReplyTo)] = null,
            [nameof(IBasicProperties.ReplyToAddress)] = null,
            [nameof(IBasicProperties.Timestamp)] = default(AmqpTimestamp),
            [nameof(IBasicProperties.Type)] = null,
            [nameof(IBasicProperties.UserId)] = null,
            [nameof(IBasicProperties.ProtocolClassId)] = (ushort)60,
            [nameof(IBasicProperties.ProtocolClassName)] = "basic",
        };

        // Initialize the properties and methods cache
        var props = typeof(FakeBasicProperties).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        getters = props.Select(pi => (n: pi.Name, m: pi.GetMethod)).ToDictionary(x => x.n, x => x.m);
        setters = props.Where(pi => pi.SetMethod != null).Select(pi => (n: pi.Name, m: pi.SetMethod)).ToDictionary(x => x.n, x => x.m);
        var methods = typeof(FakeBasicProperties).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        isPresentMethods = new Dictionary<string, MethodInfo>();
        foreach (var propname in setters.Keys)
        {
            var method = methods.SingleOrDefault(mi => mi.Name == $"Is{propname}Present");
            if (method != null)
                isPresentMethods.Add(propname, method);
        }

        clearMethods = new Dictionary<string, MethodInfo>();
        foreach (var propname in setters.Keys)
        {
            var method = methods.SingleOrDefault(mi => mi.Name == $"Clear{propname}");
            if (method != null)
                clearMethods.Add(propname, method);
        }
    }

    [Fact]
    public void FakeBasicProperties_are_initially_not_set()
    {
        var instance = new FakeBasicProperties();
        foreach (var propname in initialValues.Keys)
        {
            var value = getters[propname].Invoke(instance, null);
            Assert.True(Equals(initialValues[propname], value), $"Different values for property {propname}");
        }
    }

    [Fact]
    public void FakeBasicProperties_IsPresent_initially_returns_false()
    {
        var instance = new FakeBasicProperties();
        foreach (var (name, method) in isPresentMethods)
            Assert.False((bool)method.Invoke(instance, null), $"{name} did not return false");
    }

    [Theory]
    [InlineData(nameof(IBasicProperties.AppId), "AppId")]
    [InlineData(nameof(IBasicProperties.ClusterId), "ClusterId")]
    [InlineData(nameof(IBasicProperties.ContentEncoding), "ContentEncoding")]
    [InlineData(nameof(IBasicProperties.ContentType), "ContentType")]
    [InlineData(nameof(IBasicProperties.CorrelationId), "CorrelationId")]
    [InlineData(nameof(IBasicProperties.DeliveryMode), (byte)255)]
    [InlineData(nameof(IBasicProperties.Expiration), "Expiration")]
    [InlineData(nameof(IBasicProperties.MessageId), "MessageId")]
    [InlineData(nameof(IBasicProperties.Persistent), true)]
    [InlineData(nameof(IBasicProperties.Priority), (byte)255)]
    [InlineData(nameof(IBasicProperties.ReplyTo), "ReplyTo")]
    [InlineData(nameof(IBasicProperties.Type), "Type")]
    [InlineData(nameof(IBasicProperties.UserId), "UserId")]
    public void FakeBasicProperties_retains_set_values(string propname, object value)
    {
        var instance = new FakeBasicProperties();
        _ = setters[propname].Invoke(instance, new[] { value });
        var actual = getters[propname].Invoke(instance, null);

        Assert.True(Equals(value, actual), $"Different values for property {propname}");
    }

    [Theory]
    [InlineData(nameof(IBasicProperties.AppId), "AppId")]
    [InlineData(nameof(IBasicProperties.ClusterId), "ClusterId")]
    [InlineData(nameof(IBasicProperties.ContentEncoding), "ContentEncoding")]
    [InlineData(nameof(IBasicProperties.ContentType), "ContentType")]
    [InlineData(nameof(IBasicProperties.CorrelationId), "CorrelationId")]
    [InlineData(nameof(IBasicProperties.DeliveryMode), (byte)255)]
    [InlineData(nameof(IBasicProperties.Expiration), "Expiration")]
    [InlineData(nameof(IBasicProperties.MessageId), "MessageId")]
    [InlineData(nameof(IBasicProperties.Priority), (byte)255)]
    [InlineData(nameof(IBasicProperties.ReplyTo), "ReplyTo")]
    [InlineData(nameof(IBasicProperties.Type), "Type")]
    [InlineData(nameof(IBasicProperties.UserId), "UserId")]
    public void FakeBasicProperties_IsPresent_returns_false_after_clear(string propname, object value)
    {
        var instance = new FakeBasicProperties();
        _ = setters[propname].Invoke(instance, new[] { value });
        _ = clearMethods[propname].Invoke(instance, null);
        var isPresent = (bool)isPresentMethods[propname].Invoke(instance, null);

        Assert.False(isPresent, $"IsPresent did not return false for {propname}");
    }

    [Theory]
    [InlineData(false, (byte)1)]
    [InlineData(true, (byte)2)]
    public void Setting_persistent_modifies_DeliveryMode(bool persistent, byte expectedDeliveryMode)
    {
        var instance = new FakeBasicProperties { Persistent = persistent };
        Assert.Equal(expectedDeliveryMode, instance.DeliveryMode);
    }

    [Fact]
    public void Setting_ReplyToAddress_modifies_ReplyTo()
    {
        var instance = new FakeBasicProperties { ReplyToAddress = new PublicationAddress("type", "name", "key") };
        Assert.Equal("type://name/key", instance.ReplyTo);
    }

    [Fact]
    public void IsHeadersPresent_returns_false_after_ClearHeaders()
    {
        var instance = new FakeBasicProperties { Headers = new Dictionary<string, object>() };
        Assert.True(instance.IsHeadersPresent());
        instance.ClearHeaders();
        Assert.False(instance.IsHeadersPresent());
    }

    [Fact]
    public void IsTimestampPresent_returns_false_after_ClearTimestamp()
    {
        var instance = new FakeBasicProperties { Timestamp = new AmqpTimestamp(123456789L) };
        Assert.True(instance.IsTimestampPresent());
        instance.ClearTimestamp();
        Assert.False(instance.IsTimestampPresent());
    }
}
