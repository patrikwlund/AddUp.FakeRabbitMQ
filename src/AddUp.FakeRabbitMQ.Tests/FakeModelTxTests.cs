using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace AddUp.RabbitMQ.Fakes;

[ExcludeFromCodeCoverage]
public class FakeModelTxTests
{
    [Fact]
    public void TxSelect_does_nothing_because_it_is_not_implemented_yet()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
            model.TxSelect();

        Assert.True(true);
    }

    [Fact]
    public void TxCommit_does_nothing_because_it_is_not_implemented_yet()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
            model.TxCommit();

        Assert.True(true);
    }

    [Fact]
    public void TxRollback_does_nothing_because_it_is_not_implemented_yet()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
            model.TxRollback();

        Assert.True(true);
    }
}
