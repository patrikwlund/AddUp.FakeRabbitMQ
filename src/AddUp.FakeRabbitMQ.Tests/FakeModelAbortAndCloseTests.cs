using System.Diagnostics.CodeAnalysis;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Xunit;
using System.Linq;

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
        public void BasicClose_unregisters_consumers()
        {
            var server = new RabbitServer();
            var model = new FakeModel(server);
            model.QueueDeclare("my_queue");
            var expectedConsumerTag = "foo";
            var actualConsumerTag = "";

            var consumer = new EventingBasicConsumer(model);
            var receivedHasRan = false;
            consumer.Received += (s, e) => receivedHasRan = true;
            consumer.Unregistered += (s, e) => actualConsumerTag = e.ConsumerTags.First();

            model.BasicConsume("my_queue", false, expectedConsumerTag, consumer);
            Assert.True(consumer.IsRunning);

            model.Close();
            model.Dispose();

            Assert.False(consumer.IsRunning);

            using (var sendModel = new FakeModel(server))
            {
                model.BasicPublish("", "my_queue", model.CreateBasicProperties(), Encoding.ASCII.GetBytes("hello"));
            }

            Assert.Equal(expectedConsumerTag, actualConsumerTag);
            Assert.Equal(0, server.Queues["my_queue"].ConsumerCount);
            Assert.False(receivedHasRan);
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
