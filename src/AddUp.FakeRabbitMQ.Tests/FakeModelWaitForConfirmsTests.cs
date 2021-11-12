using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace AddUp.RabbitMQ.Fakes
{
    [ExcludeFromCodeCoverage]
    public class FakeModelWaitForConfirmsTests
    {
        [Fact]
        public void WaitForConfirms_throws_if_ConfirmSelect_was_not_called()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
                Assert.Throws<InvalidOperationException>(() => model.WaitForConfirms());
        }

        [Fact]
        public void WaitForConfirmsOrDie_returns_true_if_ConfirmSelect_was_called()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.ConfirmSelect();
                var result = model.WaitForConfirms();
                Assert.True(result);
            }
        }

        [Fact]
        public void WaitForConfirmsOrDie_throws_if_ConfirmSelect_was_not_called()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
                Assert.Throws<InvalidOperationException>(() => model.WaitForConfirmsOrDie());
        }

        [Fact]
        public void WaitForConfirmsOrDie_does_not_throw_if_ConfirmSelect_was_called()
        {
            var server = new RabbitServer();
            using (var model = new FakeModel(server))
            {
                model.ConfirmSelect();
                model.WaitForConfirmsOrDie();
            }

            Assert.True(true);
        }
    }
}
