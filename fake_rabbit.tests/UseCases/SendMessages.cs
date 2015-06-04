using System.Text;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Fakes.models;

namespace RabbitMQ.Fakes.Tests.UseCases
{
    [TestFixture]
    public class SendMessages
    {
        [Test]
        public void SendToExchangeOnly()
        {
            var rabbitServer = new RabbitServer();
            var connectionFactory = new FakeConnectionFactory(rabbitServer);

            using (var connection = connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                const string message = "hello world!";
                var messageBody = Encoding.ASCII.GetBytes(message);
                channel.BasicPublish(exchange:"my_exchange",routingKey:null,mandatory:false,basicProperties:null,body:messageBody);
            }

            Assert.That(rabbitServer.Exchanges["my_exchange"].Messages.Count,Is.EqualTo(1));
        }

        [Test]
        public void SendToExchangeWithBoundQueue()
        {
            var rabbitServer = new RabbitServer();
            var connectionFactory = new FakeConnectionFactory(rabbitServer);

            ConfigureQueueBinding(rabbitServer,"my_exchange","some_queue");

            using (var connection = connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                const string message = "hello world!";
                var messageBody = Encoding.ASCII.GetBytes(message);
                channel.BasicPublish(exchange: "my_exchange", routingKey: null, mandatory: false, basicProperties: null, body: messageBody);
            }

            Assert.That(rabbitServer.Queues["some_queue"].Messages.Count, Is.EqualTo(1));
        }

        [Test]
        public void SendToExchangeWithMultipleBoundQueues()
        {
            var rabbitServer = new RabbitServer();
            var connectionFactory = new FakeConnectionFactory(rabbitServer);

            ConfigureQueueBinding(rabbitServer, "my_exchange", "some_queue");
            ConfigureQueueBinding(rabbitServer, "my_exchange", "some_other_queue");

            using (var connection = connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                const string message = "hello world!";
                var messageBody = Encoding.ASCII.GetBytes(message);
                channel.BasicPublish(exchange: "my_exchange", routingKey: null, mandatory: false, basicProperties: null, body: messageBody);
            }

            Assert.That(rabbitServer.Queues["some_queue"].Messages.Count, Is.EqualTo(1));
            Assert.That(rabbitServer.Queues["some_other_queue"].Messages.Count, Is.EqualTo(1));
        }

        private void ConfigureQueueBinding(RabbitServer rabbitServer, string exchangeName, string queueName)
        {
            var connectionFactory = new FakeConnectionFactory(rabbitServer);
            using (var connection = connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue:queueName,durable:false,exclusive:false,autoDelete:false,arguments:null);
                channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Direct);

                channel.QueueBind(queueName,exchangeName,null);
            }
        }
    }
}