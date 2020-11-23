using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace AddUp.RabbitMQ.Fakes.issues
{
    [ExcludeFromCodeCoverage]
    public class CloseModelTests
    {
        // This ensures issue #25 is fixed
        [Fact]
        public void Closing_a_model_after_disconnection_should_not_throw()
        {
            var factory = new FakeConnectionFactory();
            var connection = factory.CreateConnection();

            var model = connection.CreateModel();
            connection.Close();
            model.Close();

            Assert.True(true); // If we didn't throw, we reached this point.
        }
    }
}
