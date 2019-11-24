using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AddUp.RabbitMQ.Fakes
{
    internal sealed class RabbitQueue
    {
        public RabbitQueue()
        {
            Messages = new ConcurrentQueue<RabbitMessage>();
            Bindings = new ConcurrentDictionary<string, RabbitExchangeQueueBinding>();
            Arguments = new Dictionary<string, object>();
        }

        public event EventHandler<RabbitMessage> MessagePublished;

        public ConcurrentQueue<RabbitMessage> Messages { get; }
        public ConcurrentDictionary<string, RabbitExchangeQueueBinding> Bindings { get; }
        public IDictionary<string, object> Arguments { get; set; }
        public string Name { get; set; }
        public bool IsDurable { get; set; }
        public bool IsExclusive { get; set; }
        public bool IsAutoDelete { get; set; }

        public void PublishMessage(RabbitMessage message)
        {
            var queueMessage = message.Copy();
            queueMessage.Queue = Name;
            Messages.Enqueue(queueMessage);
            MessagePublished?.Invoke(this, queueMessage);
        }
        
        public void ClearMessages()
        {
            while (Messages.TryDequeue(out _))
                ;
        }
    }
}