using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes
{
    internal sealed class ConsumerData(IAsyncBasicConsumer consumer, RabbitQueue queue, Func<RabbitMessage, CancellationToken, Task> queueMessagePublished)
    {
        public IAsyncBasicConsumer Consumer { get; } = consumer;
        public RabbitQueue Queue { get; } = queue;
        public Func<RabbitMessage, CancellationToken, Task> QueueMessagePublished { get; } = queueMessagePublished;
    }
}
