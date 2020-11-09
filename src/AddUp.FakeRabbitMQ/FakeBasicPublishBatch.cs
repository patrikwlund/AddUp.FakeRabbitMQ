using System.Collections.Generic;
using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes
{
    internal sealed class FakeBasicPublishBatch : IBasicPublishBatch
    {
        private readonly List<(string, string, bool, IBasicProperties, byte[])> messages = new List<(string, string, bool, IBasicProperties, byte[])>();
        private readonly IModel model;

        public FakeBasicPublishBatch(IModel parentModel) =>  model = parentModel;

        public void Add(string exchange, string routingKey, bool mandatory, IBasicProperties properties, byte[] body) =>
            messages.Add((exchange, routingKey, mandatory, properties, body));

        public void Publish()
        {
            foreach (var (exchange, routingKey, mandatory, properties, body) in messages)
                model.BasicPublish(exchange, routingKey, mandatory, properties, body);
        }
    }
}
