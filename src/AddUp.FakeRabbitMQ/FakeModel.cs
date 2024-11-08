using System.Collections.Concurrent;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace AddUp.RabbitMQ.Fakes
{
    internal sealed class FakeModel : IChannel
    {
        private readonly ConcurrentDictionary<ulong, RabbitMessage> workingMessages = new();
        private readonly ConcurrentDictionary<string, ConsumerData> consumers = new();

        private readonly ConsumerDeliveryQueue deliveryQueue;
        private readonly RabbitServer server;
        private long lastDeliveryTag;

        public FakeModel(RabbitServer rabbitServer)
        {
            server = rabbitServer;
            deliveryQueue = ConsumerDeliveryQueue.Create(
                this,
                deliveryExceptionHandler: args => CallbackException(this, args),
                rabbitServer.BlockingDeliveryMode);
        }

#pragma warning disable 67
        public event AsyncEventHandler<BasicAckEventArgs> BasicAcksAsync;
        public event AsyncEventHandler<BasicNackEventArgs> BasicNacksAsync;
        public event AsyncEventHandler<BasicReturnEventArgs> BasicReturnAsync;
        public event AsyncEventHandler<CallbackExceptionEventArgs> CallbackException;
        public event AsyncEventHandler<FlowControlEventArgs> FlowControlAsync;
        public event AsyncEventHandler<ShutdownEventArgs> ChannelShutdownAsync;
        public event AsyncEventHandler<CallbackExceptionEventArgs> CallbackExceptionAsync;
#pragma warning restore 67

        public int ChannelNumber { get; }
        public IAsyncBasicConsumer DefaultConsumer { get; set; }
        public ulong NextPublishSeqNo { get; set; }
        public TimeSpan ContinuationTimeout { get; set; }
        public ShutdownEventArgs CloseReason { get; private set; }
        public string CurrentQueue { get; private set; }
        public bool IsOpen => CloseReason == null;
        public bool IsClosed => !IsOpen;

        internal ConcurrentDictionary<ulong, RabbitMessage> WorkingMessagesForUnitTests => workingMessages;

        public ValueTask<ulong> GetNextPublishSequenceNumberAsync(CancellationToken cancellationToken = default)
            => new ValueTask<ulong>(NextPublishSeqNo);

        public ValueTask BasicAckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken = default)
        {
            void ack(ulong dtag)
            {
                _ = workingMessages.TryRemove(dtag, out var message);
                if (message == null) return;

                _ = server.Queues.TryGetValue(message.Queue, out var queue);
                if (queue == null) return;

                queue.Ack(dtag);
            }

            if (multiple)
            {
                var dtag = deliveryTag;
                while (dtag > 0)
                {
                    ack(dtag);
                    dtag--;
                }
            }
            else ack(deliveryTag);

            return default;
        }

        public ValueTask BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken = default)
        {
            void nack(ulong dtag)
            {
                RabbitMessage message;
                if (requeue)
                    _ = workingMessages.TryGetValue(dtag, out message);
                else
                    _ = workingMessages.TryRemove(dtag, out message);

                if (message == null) return;

                _ = server.Queues.TryGetValue(message.Queue, out var queue);
                if (queue == null) return;

                if (!requeue) queue.Ack(deliveryTag);
            }

            if (multiple)
            {
                var dtag = deliveryTag;
                while (dtag > 0)
                {
                    nack(dtag);
                    dtag--;
                }
            }
            else nack(deliveryTag);

            return default;
        }

        // BasicReject is simply BasicNack with no possibility for multiple rejections
        public ValueTask BasicRejectAsync(ulong deliveryTag, bool requeue, CancellationToken cancellationToken = default) => BasicNackAsync(deliveryTag, false, requeue, cancellationToken);

        public async Task BasicCancelAsync(string consumerTag, bool noWait = false, CancellationToken cancellationToken = default)
        {
            _ = consumers.TryRemove(consumerTag, out var consumerData);
            if (consumerData == null) return;

            consumerData.Queue.MessagePublished -= consumerData.QueueMessagePublished;

            await consumerData.Consumer.HandleBasicCancelOkAsync(consumerTag, cancellationToken);
        }

        public async Task<string> BasicConsumeAsync(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments, IAsyncBasicConsumer consumer, CancellationToken cancellationToken = default)
        {
            async Task notifyConsumerOfMessage(RabbitMessage message, CancellationToken cancellationToken = default)
            {
                var deliveryTag = message.DeliveryTag;

                const bool redelivered = false;
                var exchange = message.Exchange;
                var routingKey = message.RoutingKey;
                var basicProperties = message.BasicProperties ?? new BasicProperties();
                var body = message.Body;

                RabbitMessage updateFunction(ulong key, RabbitMessage existingMessage) => existingMessage;
                _ = workingMessages.AddOrUpdate(deliveryTag, message, updateFunction);

                await consumer.HandleBasicDeliverAsync(
                    consumerTag, deliveryTag, redelivered, exchange, routingKey, basicProperties, body, cancellationToken);

                if (autoAck)
                    await BasicAckAsync(deliveryTag, false, cancellationToken);
            }

            // Deliberately check for empty string here, latest RabbitMQ client accepts ""
            // but will throw on null and kill the channel.
            if (consumerTag == "")
            {
                var guidString = Guid.NewGuid();
                // https://www.rabbitmq.com/amqp-0-9-1-reference.html#basic.consume.consumer-tag
                // If this field is empty the server will generate a unique tag.
                consumerTag = $"amq.{guidString:N}";
            }

            _ = server.Queues.TryGetValue(queue, out var queueInstance);
            if (queueInstance != null)
            {
                var consumerData = new ConsumerData(consumer, queueInstance, (message, ct) =>
                    deliveryQueue.Deliver(ct => notifyConsumerOfMessage(message, ct), ct));

                // https://www.rabbitmq.com/amqp-0-9-1-reference.html#basic.consume.consumer-tag
                // The client MUST NOT specify a tag that refers to an existing consumer. Error code: not-allowed
                ConsumerData updateFunction(string s, ConsumerData _) =>
                    throw new OperationInterruptedException(
                        new ShutdownEventArgs(ShutdownInitiator.Peer, 530, $"NOT_ALLOWED - attempt to reuse consumer tag '{s}'"));
                _ = consumers.AddOrUpdate(consumerTag, consumerData, updateFunction);

                foreach (var message in queueInstance.GetMessages())
                    await consumerData.QueueMessagePublished(message, cancellationToken);

                queueInstance.MessagePublished += consumerData.QueueMessagePublished;

                await consumer.HandleBasicConsumeOkAsync(consumerTag);
            }

            return consumerTag;
        }

        public Task<BasicGetResult> BasicGetAsync(string queue, bool autoAck, CancellationToken cancellationToken = default)
        {
            _ = server.Queues.TryGetValue(queue, out var queueInstance);
            if (queueInstance == null) return Task.FromResult<BasicGetResult>(null);

            queueInstance.TryGet(out var message, autoAck);
            if (message == null) return Task.FromResult<BasicGetResult>(null);

            var deliveryTag = message.DeliveryTag;

            const bool redelivered = false;
            var exchange = message.Exchange;
            var routingKey = message.RoutingKey;
            var messageCount = Convert.ToUInt32(queueInstance.MessageCount);
            var basicProperties = message.BasicProperties ?? new BasicProperties();
            var body = message.Body;

            if (autoAck)
                _ = workingMessages.TryRemove(deliveryTag, out _);
            else
            {
                RabbitMessage updateFunction(ulong key, RabbitMessage existingMessage) => existingMessage;
                _ = workingMessages.AddOrUpdate(deliveryTag, message, updateFunction);
            }

            return Task.FromResult(new BasicGetResult(deliveryTag, redelivered, exchange, routingKey, messageCount, basicProperties, body));
        }

        public async ValueTask BasicPublishAsync<TProperties>(string exchange, string routingKey, bool mandatory, TProperties basicProperties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default) where TProperties : IReadOnlyBasicProperties, IAmqpHeader
        {
            // Let's create the delivery tag as soon as publishing so that we can find it in the queues later on
            _ = Interlocked.Increment(ref lastDeliveryTag);
            var deliveryTag = Convert.ToUInt64(lastDeliveryTag);

            var message = new RabbitMessage(deliveryTag)
            {
                Exchange = exchange,
                RoutingKey = routingKey,
                Mandatory = mandatory,
                BasicProperties = basicProperties,
                Body = body.ToArray()
            };

            var exchangeInstance = server.Exchanges.GetOrAdd(exchange, exchange => new(ExchangeType.Direct, server)
            {
                Name = exchange,
                Arguments = null,
                AutoDelete = false,
                IsDurable = false
            });

            await exchangeInstance.PublishMessage(message, CancellationToken.None);

            if (NextPublishSeqNo != 0ul)
                NextPublishSeqNo++;
        }

        public ValueTask BasicPublishAsync<TProperties>(CachedString exchange, CachedString routingKey, bool mandatory, TProperties basicProperties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default) where TProperties : IReadOnlyBasicProperties, IAmqpHeader
        {
            return BasicPublishAsync(exchange.Value, routingKey.Value, mandatory, basicProperties, body, cancellationToken);
        }

        public Task BasicQosAsync(uint prefetchSize, ushort prefetchCount, bool global, CancellationToken cancellationToken = default)
        {
            // Fake implementation. Nothing to do here.
            return Task.CompletedTask;
        }

        public async Task CloseAsync(ShutdownEventArgs reason, bool abort, CancellationToken cancellationToken = default)
        {
            if (CloseReason is null)
            {
                try
                {
                    CloseReason = reason;

                    var consumerTags = consumers.Keys.ToList();
                    foreach (var consumerTag in consumerTags)
                        await BasicCancelAsync(consumerTag, noWait: false, cancellationToken);

                    deliveryQueue.Complete();
                    if (ChannelShutdownAsync != null)
                    {
                        await ChannelShutdownAsync.Invoke(this, reason);
                    }
                }
                catch
                {
                    if (!abort) throw;
                }
            }

            deliveryQueue.WaitForCompletion();
        }

        public Task CloseAsync(ushort replyCode, string replyText, bool abort, CancellationToken cancellationToken = default)
        {
            var reason = new ShutdownEventArgs(ShutdownInitiator.Application, replyCode, replyText, cancellationToken);
            return CloseAsync(reason, abort, cancellationToken);
        }

        public async Task<uint> ConsumerCountAsync(string queue, CancellationToken cancellationToken = default)
        {
            var ok = await QueueDeclarePassiveAsync(queue, cancellationToken);
            return ok.ConsumerCount;
        }

        public void Dispose()
        {
            if (IsOpen)
            {
                this.AbortAsync().GetAwaiter().GetResult();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await this.AbortAsync();
        }

        public Task ExchangeBindAsync(string destination, string source, string routingKey, IDictionary<string, object> arguments = default, bool noWait = false, CancellationToken cancellationToken = default)
        {
            _ = server.Exchanges.TryGetValue(source, out var exchange);
            _ = server.Queues.TryGetValue(destination, out var queue);

            var binding = new RabbitExchangeQueueBinding { Exchange = exchange, Queue = queue, RoutingKey = routingKey };
            if (exchange != null)
                _ = exchange.Bindings.AddOrUpdate(binding.Key, binding, (k, v) => binding);
            if (queue != null)
                _ = queue.Bindings.AddOrUpdate(binding.Key, binding, (k, v) => binding);

            return Task.CompletedTask;
        }

        public Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments = default, bool passive = false, bool noWait = false, CancellationToken cancellationToken = default)
        {
            var exchangeInstance = new RabbitExchange(type, server)
            {
                Name = exchange,
                IsDurable = durable,
                AutoDelete = autoDelete,
                Arguments = arguments
            };

            _ = server.Exchanges.TryAdd(exchange, exchangeInstance);

            return Task.CompletedTask;
        }

        public Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken = default)
        {
            if (server.Exchanges.ContainsKey(exchange)) return Task.CompletedTask;

            var shutdownArgs = new ShutdownEventArgs(initiator: ShutdownInitiator.Peer,
                replyText: $"NOT_FOUND - no exchange '{exchange}' in vhost '/'",
                replyCode: 404,
                classId: 40,
                methodId: 10);

            throw new OperationInterruptedException(shutdownArgs);
        }

        public Task ExchangeDeleteAsync(string exchange, bool ifUnused = false, bool noWait = false, CancellationToken cancellationToken = default)
        {
            server.Exchanges.TryRemove(exchange, out _);
            return Task.CompletedTask;
        }

        public Task ExchangeUnbindAsync(string destination, string source, string routingKey, IDictionary<string, object> arguments = default, bool noWait = false, CancellationToken cancellationToken = default)
        {
            _ = server.Exchanges.TryGetValue(source, out var exchange);
            _ = server.Queues.TryGetValue(destination, out var queue);

            var binding = new RabbitExchangeQueueBinding { Exchange = exchange, Queue = queue, RoutingKey = routingKey };
            if (exchange != null)
                _ = exchange.Bindings.TryRemove(binding.Key, out _);
            if (queue != null)
                _ = queue.Bindings.TryRemove(binding.Key, out _);

            return Task.CompletedTask;
        }

        public async Task<uint> MessageCountAsync(string queue, CancellationToken cancellationToken = default)
        {
            var ok = await QueueDeclarePassiveAsync(queue, cancellationToken);
            return ok.MessageCount;
        }

        public Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object> arguments = default, bool noWait = false, CancellationToken cancellationToken = default) =>
            ExchangeBindAsync(queue, exchange, routingKey, arguments, noWait, cancellationToken);

        public async Task<QueueDeclareOk> QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments = default, bool passive = false, bool noWait = false, CancellationToken cancellationToken = default)
        {
            // This handles 'default' queues creations with constructs such as:
            // var queueName = Channel.QueueDeclare(); // temporary anonymous queue
            var q = string.IsNullOrEmpty(queue) ? Guid.NewGuid().ToString() : queue;

            var queueInstance = new RabbitQueue
            {
                Name = q,
                IsDurable = durable,
                IsExclusive = exclusive,
                IsAutoDelete = autoDelete,
                Arguments = arguments
            };

            RabbitQueue updateFunction(string name, RabbitQueue existing) => existing;
            _ = server.Queues.AddOrUpdate(q, queueInstance, updateFunction);

            // RabbitMQ automatically binds queues to the default exchange.
            // https://www.rabbitmq.com/tutorials/amqp-concepts.html#exchange-default
            await QueueBindAsync(q, "", q, null, false, cancellationToken);

            var result = new QueueDeclareOk(q, 0u, 0u);
            CurrentQueue = result.QueueName;
            return result;
        }

        public Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken = default)
        {
            if (server.Queues.TryGetValue(queue, out var rabbitQueue))
            {
                var result = new QueueDeclareOk(queue,
                    (uint)unchecked(rabbitQueue.MessageCount),
                    (uint)unchecked(rabbitQueue.ConsumerCount));

                CurrentQueue = result.QueueName;
                return Task.FromResult(result);
            }

            var shutdownArgs = new ShutdownEventArgs(initiator: ShutdownInitiator.Peer,
                    replyText: $"NOT_FOUND - no queue '{queue}' in vhost '/'",
                    replyCode: 404,
                    classId: 50,
                    methodId: 10);

            throw new OperationInterruptedException(shutdownArgs);
        }

        public Task<uint> QueueDeleteAsync(string queue, bool ifUnused, bool ifEmpty, bool noWait = false, CancellationToken cancellationToken = default)
        {
            _ = server.Queues.TryRemove(queue, out var instance);
            var messageCount = (uint?)instance?.MessageCount ?? 0u;
            return Task.FromResult(messageCount);
        }

        public Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken = default)
        {
            _ = server.Queues.TryGetValue(queue, out var instance);
            if (instance == null)
                return Task.FromResult(0u);

            var messageCount = instance.ClearMessages();

            return Task.FromResult(messageCount);
        }

        public Task QueueUnbindAsync(string queue, string exchange, string routingKey, IDictionary<string, object> arguments = default, CancellationToken cancellationToken = default) =>
            ExchangeUnbindAsync(queue, exchange, routingKey, arguments, false, cancellationToken);

        public Task TxCommitAsync(CancellationToken cancellationToken = default)
        {
            // Fake implementation. Nothing to do here.
            return Task.CompletedTask;
        }

        public Task TxRollbackAsync(CancellationToken cancellationToken = default)
        {
            // Fake implementation. Nothing to do here.
            return Task.CompletedTask;
        }

        public Task TxSelectAsync(CancellationToken cancellationToken = default)
        {
            // Fake implementation. Nothing to do here.
            return Task.CompletedTask;
        }
    }
}