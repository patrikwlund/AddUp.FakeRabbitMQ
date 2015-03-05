using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace fake_rabbit.models
{
    public class Queue
    {
        public string Name { get; set; }

        public bool IsDurable { get; set; }

        public bool IsExclusive { get; set; }

        public bool IsAutoDelete { get; set; }

        public IDictionary Arguments = new Dictionary<string, object>();

        public ConcurrentQueue<RabbitMessage> Messages = new ConcurrentQueue<RabbitMessage>();
        public ConcurrentDictionary<string,ExchangeQueueBinding> Bindings = new ConcurrentDictionary<string,ExchangeQueueBinding>();

        public event EventHandler<RabbitMessage> MessagePublished = (sender, message) => { };  

        public void PublishMessage(RabbitMessage message)
        {
            var queueMessage = message.Copy();
            queueMessage.Queue = this.Name;

            this.Messages.Enqueue(queueMessage);

            MessagePublished(this, queueMessage);
        }
    }
}