using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace AddUp.RabbitMQ.Fakes;

[ExcludeFromCodeCoverage]
public class FakeModelTxTests
{
    [Fact]
    public async Task TxSelect_does_nothing_because_it_is_not_implemented_yet()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
            await model.TxSelectAsync();

        Assert.True(true);
    }

    [Fact]
    public async Task TxCommit_does_nothing_because_it_is_not_implemented_yet()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
            await model.TxCommitAsync();

        Assert.True(true);
    }

    [Fact]
    public async Task TxRollback_does_nothing_because_it_is_not_implemented_yet()
    {
        var server = new RabbitServer();
        using (var model = new FakeModel(server))
            await model.TxRollbackAsync();

        Assert.True(true);
    }
}
