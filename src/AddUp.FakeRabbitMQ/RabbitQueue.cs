using System.Collections.Concurrent;

namespace AddUp.RabbitMQ.Fakes
{
    public sealed class RabbitQueue
    {
        private readonly object me = new();
        private readonly ConcurrentDictionary<ulong, RabbitMessage> messages = new();
        private readonly ConcurrentQueue<ulong> deliveryTags = new();
        private readonly HashSet<Func<RabbitMessage, CancellationToken, Task>> messagePublishedEventHandlers = [];

        public event Func<RabbitMessage, CancellationToken, Task> MessagePublished
        {
            add => messagePublishedEventHandlers.Add(value);
            remove => messagePublishedEventHandlers.Remove(value);
        }

        public ConcurrentDictionary<string, RabbitExchangeQueueBinding> Bindings { get; } = [];
        public IDictionary<string, object> Arguments { get; set; }
        public string Name { get; set; }
        public bool IsDurable { get; set; }
        public bool IsExclusive { get; set; }
        public bool IsAutoDelete { get; set; }
        public int MessageCount => messages.Count;
        public bool HasMessages => !messages.IsEmpty;
        public int ConsumerCount => messagePublishedEventHandlers.Count;

        public void Ack(ulong deliveryTag)
        {
            lock (me)
                _ = messages.TryRemove(deliveryTag, out _);
        }

        public void Enqueue(RabbitMessage message)
        {
            var deliveryTag = message.DeliveryTag;
            if (deliveryTag == 0) throw new InvalidOperationException("No Delivery Tag");

            RabbitMessage updateFunction(ulong key, RabbitMessage existingMessage) => existingMessage;
            lock (me)
            {
                _ = messages.AddOrUpdate(deliveryTag, message, updateFunction);
                deliveryTags.Enqueue(deliveryTag);
            }
        }

        public IEnumerable<RabbitMessage> GetMessages()
        {
            while (deliveryTags.Count > 0) 
            {
                if (TryGet(out var m, false))
                    yield return m;
            }
        }

        public bool TryGet(out RabbitMessage result, bool remove)
        {
            result = null;
            lock (me)
            {
                var found = deliveryTags.TryDequeue(out var deliveryTag);
                return found && (remove
                    ? messages.TryRemove(deliveryTag, out result)
                    : messages.TryGetValue(deliveryTag, out result));
            }
        }

        public async Task PublishMessage(RabbitMessage message, CancellationToken cancellationToken = default)
        {
            var queueMessage = message.Copy();
            queueMessage.Queue = Name;
            Enqueue(queueMessage);
            foreach (var handler in messagePublishedEventHandlers)
                await handler(queueMessage, cancellationToken);
        }

        public uint ClearMessages()
        {
            var count = 0u;
            while (TryGet(out _, true))
                count++;
            return count;
        }

        internal bool TryPeekForUnitTests(out RabbitMessage result)
        {
            result = null;
            lock (me)
            {
                var found = deliveryTags.TryPeek(out var deliveryTag);
                return found && messages.TryGetValue(deliveryTag, out result);
            }
        }

        internal RabbitMessage[] GetAllMessagesForUnitTests() => messages.Values.ToArray();
    }
}