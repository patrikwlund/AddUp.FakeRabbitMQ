using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace AddUp.RabbitMQ.Fakes
{
    [ExcludeFromCodeCoverage]
    public class FakeModelAbortAndCloseTests
    {
        [Fact]
        public void Close_closes_the_channel()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.Close();

                Assert.True(model.IsClosed);
                Assert.False(model.IsOpen);
                Assert.NotNull(model.CloseReason);
            }
        }

        [Fact]
        public void Close_with_arguments_closes_the_channel()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.Close(5, "some message");

                Assert.True(model.IsClosed);
                Assert.False(model.IsOpen);
                Assert.NotNull(model.CloseReason);
            }
        }

        [Fact]
        public void Abort_closes_the_channel()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.Abort();

                Assert.True(model.IsClosed);
                Assert.False(model.IsOpen);
                Assert.NotNull(model.CloseReason);
            }
        }

        [Fact]
        public void Abort_with_arguments_closes_the_channel()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.Abort(5, "some message");

                Assert.True(model.IsClosed);
                Assert.False(model.IsOpen);
                Assert.NotNull(model.CloseReason);
            }
        }
    }
}
