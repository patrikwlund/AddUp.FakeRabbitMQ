using System.Text;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Fakes.models;

namespace RabbitMQ.Fakes.Tests.UseCases
{
    [TestFixture]
    public class ReceiveMessages
    {
        [Test]
        public void ReceiveMessagesOnQueue()
        {
            var rabbitServer = new RabbitServer();

            ConfigureQueueBinding(rabbitServer, "my_exchange", "my_queue");
            SendMessage(rabbitServer,"my_exchange","hello_world");

            var connectionFactory = new FakeConnectionFactory(rabbitServer);
            using (var connection = connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // First message
                var message = channel.BasicGet("my_queue", noAck: false);
                
                Assert.That(message,Is.Not.Null);
                var messageBody = Encoding.ASCII.GetString(message.Body);

                Assert.That(messageBody,Is.EqualTo("hello_world"));

                channel.BasicAck(message.DeliveryTag,multiple:false);
            }

        }

        private static void SendMessage(RabbitServer rabbitServer, string exchange, string message)
        {
            var connectionFactory = new FakeConnectionFactory(rabbitServer);

            using (var connection = connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var messageBody = Encoding.ASCII.GetBytes(message);
                channel.BasicPublish(exchange: exchange, routingKey: null, mandatory: false, basicProperties: null,body: messageBody);
            }
        }

        private void ConfigureQueueBinding(RabbitServer rabbitServer, string exchangeName, string queueName)
        {
            var connectionFactory = new FakeConnectionFactory(rabbitServer);
            using (var connection = connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Direct);

                channel.QueueBind(queueName, exchangeName, null);
            }
        }
    }
}