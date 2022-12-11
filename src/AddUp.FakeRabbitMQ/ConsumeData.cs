using System;
using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes
{
    internal class ConsumerData
    {
        public ConsumerData(IBasicConsumer consumer, RabbitQueue queue, EventHandler<RabbitMessage> queueMessagePublished)
        {
            Consumer = consumer;
            Queue = queue;
            QueueMessagePublished = queueMessagePublished;
        }

        public IBasicConsumer Consumer { get; }
        public RabbitQueue Queue { get; }
        public EventHandler<RabbitMessage> QueueMessagePublished { get; }
    }
}
