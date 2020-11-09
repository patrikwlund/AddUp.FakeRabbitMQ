using System.Collections.Generic;
using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes
{
    public class FakeBasicPublishBatch : IBasicPublishBatch
    {
        private readonly List<(string, string, bool, IBasicProperties, byte[])> _messages = new List<(string, string, bool, IBasicProperties, byte[])>();
        private readonly IModel _model;

        public FakeBasicPublishBatch(IModel model)
        {
            _model = model;
        }

        public void Add(string exchange, string routingKey, bool mandatory, IBasicProperties properties, byte[] body)
        {
            _messages.Add((exchange, routingKey, mandatory, properties, body));
        }

        public void Publish()
        {
            foreach (var (exchange, routingKey, mandatory, properties, body) in _messages)
            {
                _model.BasicPublish(exchange, routingKey, mandatory, properties, body);
            }
        }
    }
}
