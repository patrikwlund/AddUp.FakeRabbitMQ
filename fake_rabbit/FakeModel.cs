using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using fake_rabbit.models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing.v0_8;

namespace fake_rabbit
{
    public class FakeModel:IModel
    {

        public ConcurrentDictionary<string, Exchange> Exchanges = new ConcurrentDictionary<string, Exchange>();
        public ConcurrentDictionary<string, models.Queue> Queues = new ConcurrentDictionary<string, models.Queue>();

        public IEnumerable<dynamic> GetMessagesPublishedToExchange(string exchange)
        {
            Exchange exchangeInstance;
            Exchanges.TryGetValue(exchange, out exchangeInstance);

            if (exchangeInstance == null)
                return new List<dynamic>();

            return exchangeInstance.Messages;
        }

        public IEnumerable<dynamic> GetMessagesOnQueue(string queueName)
        {
            models.Queue queueInstance;
            Queues.TryGetValue(queueName, out queueInstance);

            if (queueInstance == null)
                return new List<dynamic>();

            return queueInstance.Messages;
        }

        public bool ApplyPrefetchToAllChannels { get; private set; }
        public ushort PrefetchCount { get; private set; }
        public uint PrefetchSize { get; private set; }
        public bool IsChannelFlowActive { get; private set; }

        public void Dispose()
        {
            
        }

        public IBasicProperties CreateBasicProperties()
        {
            return new BasicProperties();
        }

        public IFileProperties CreateFileProperties()
        {
            return new FileProperties();
        }

        public IStreamProperties CreateStreamProperties()
        {
            return new StreamProperties();
        }

        public void ChannelFlow(bool active)
        {
            IsChannelFlowActive = true;
        }

        public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary arguments)
        {
            var exchangeInstance = new Exchange
            {
                Name = exchange,
                Type = type,
                IsDurable = durable,
                AutoDelete = autoDelete,
                Arguments = arguments
            };
            Func<string,Exchange,Exchange> updateFunction = (name, existing) => existing;
            Exchanges.AddOrUpdate(exchange,exchangeInstance, updateFunction);
        }

        public void ExchangeDeclare(string exchange, string type, bool durable)
        {
            ExchangeDeclare(exchange, type, durable, autoDelete: false, arguments: null);
        }

        public void ExchangeDeclare(string exchange, string type)
        {
            ExchangeDeclare(exchange, type, durable:false, autoDelete: false, arguments: null);
        }

        public void ExchangeDeclarePassive(string exchange)
        {
            ExchangeDeclare(exchange, type:null, durable: false, autoDelete: false, arguments: null);
        }

        public void ExchangeDeclareNoWait(string exchange, string type, bool durable, bool autoDelete, IDictionary arguments)
        {
            ExchangeDeclare(exchange, type, durable, autoDelete: false, arguments: null);
        }

        public void ExchangeDelete(string exchange, bool ifUnused)
        {
            Exchange removedExchange;
            Exchanges.TryRemove(exchange, out removedExchange);
        }

        public void ExchangeDelete(string exchange)
        {
            ExchangeDelete(exchange, ifUnused: false);
        }

        public void ExchangeDeleteNoWait(string exchange, bool ifUnused)
        {
            ExchangeDelete(exchange, ifUnused: false);
        }

        public void ExchangeBind(string destination, string source, string routingKey, IDictionary arguments)
        {
            Exchange exchange;
            Exchanges.TryGetValue(source, out exchange);

            models.Queue queue;
            Queues.TryGetValue(destination, out queue);

            var binding = new ExchangeQueueBinding {Exchange = exchange, Queue = queue, RoutingKey = routingKey};
            if (exchange != null)
                exchange.Bindings.AddOrUpdate(binding.Key, binding, (k, v) => binding);
            if(queue!=null)
                queue.Bindings.AddOrUpdate(binding.Key, binding, (k, v) => binding);
        }

        public void ExchangeBind(string destination, string source, string routingKey)
        {
            ExchangeBind(destination:destination,source:source,routingKey:routingKey,arguments:null);
        }

        public void ExchangeUnbind(string destination, string source, string routingKey, IDictionary arguments)
        {
            Exchange exchange;
            Exchanges.TryGetValue(source, out exchange);

            models.Queue queue;
            Queues.TryGetValue(destination, out queue);

            var binding = new ExchangeQueueBinding { Exchange = exchange, Queue = queue, RoutingKey = routingKey };
            ExchangeQueueBinding removedBinding;
            if (exchange != null)
                exchange.Bindings.TryRemove(binding.Key, out removedBinding);
            if (queue != null)
                queue.Bindings.TryRemove(binding.Key,out removedBinding);
        }

        public void ExchangeUnbind(string destination, string source, string routingKey)
        {
            ExchangeUnbind(destination: destination, source: source, routingKey: routingKey, arguments: null);
        }

        public QueueDeclareOk QueueDeclare()
        {
            var name = Guid.NewGuid().ToString();
            return QueueDeclare(name, durable: false, exclusive: false, autoDelete: false, arguments: null);
        }

        public QueueDeclareOk QueueDeclarePassive(string queue)
        {
            return QueueDeclare(queue, durable: false, exclusive: false, autoDelete: false, arguments: null);
        }

        public void QueueBind(string queue, string exchange, string routingKey, IDictionary arguments)
        {
            throw new NotImplementedException();
        }

        public QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary arguments)
        {
            var queueInstance = new models.Queue
            {
                Name = queue,
                IsDurable = durable,
                IsExclusive = exclusive,
                IsAutoDelete = autoDelete,
                Arguments = arguments
            };

            Func<string,models.Queue,models.Queue> updateFunction = (name, existing) => existing;
            Queues.AddOrUpdate(queue, queueInstance, updateFunction);

            return new QueueDeclareOk(queue, 0, 0);
        }

        public void QueueDeclareNoWait(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary arguments)
        {
            QueueDeclare(queue, durable, exclusive, autoDelete, arguments);
        }

        public void QueueBind(string queue, string exchange, string routingKey)
        {
            ExchangeBind(queue, exchange, routingKey);
        }

        public void QueueUnbind(string queue, string exchange, string routingKey, IDictionary arguments)
        {
            ExchangeUnbind(queue,exchange,routingKey);
        }

        public uint QueuePurge(string queue)
        {
            models.Queue instance;
            Queues.TryRemove(queue, out instance);

            if (instance == null)
                return 0u;
            
            while (!instance.Messages.IsEmpty)
            {
                RabbitMessage itemToRemove;
                instance.Messages.TryDequeue(out itemToRemove);
            }

            return 1u;
        }

        public uint QueueDelete(string queue, bool ifUnused, bool ifEmpty)
        {
            models.Queue instance;
            Queues.TryRemove(queue, out instance);

            return instance != null ? 1u : 0u;
        }

        public void QueueDeleteNoWait(string queue, bool ifUnused, bool ifEmpty)
        {
            QueueDelete(queue,ifUnused:false,ifEmpty:false);
        }

        public uint QueueDelete(string queue)
        {
            return QueueDelete(queue, ifUnused: false, ifEmpty: false);
        }

        public void ConfirmSelect()
        {
            throw new NotImplementedException();
        }

        public bool WaitForConfirms()
        {
            throw new NotImplementedException();
        }

        public bool WaitForConfirms(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public bool WaitForConfirms(TimeSpan timeout, out bool timedOut)
        {
            throw new NotImplementedException();
        }

        public void WaitForConfirmsOrDie()
        {
            throw new NotImplementedException();
        }

        public void WaitForConfirmsOrDie(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public string BasicConsume(string queue, bool noAck, IBasicConsumer consumer)
        {
            return BasicConsume(queue: queue, noAck: noAck, consumerTag: null, noLocal: true, exclusive: false, arguments: null, consumer: consumer);      
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, IBasicConsumer consumer)
        {
           return BasicConsume(queue:queue,noAck:noAck,consumerTag:consumerTag,noLocal:true,exclusive:false,arguments:null,consumer:consumer);        
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, IDictionary arguments, IBasicConsumer consumer)
        {
            return BasicConsume(queue: queue, noAck: noAck, consumerTag: consumerTag, noLocal: true, exclusive: false, arguments: arguments, consumer: consumer);        
        }

        public string BasicConsume(string queue, bool noAck, string consumerTag, bool noLocal, bool exclusive, IDictionary arguments, IBasicConsumer consumer)
        {
            throw new NotImplementedException();

        }

        public void BasicCancel(string consumerTag)
        {
            throw new NotImplementedException();
        }

        private long _lastDeliveryTag = 0;
        private readonly ConcurrentDictionary<ulong,dynamic> _workingMessages = new ConcurrentDictionary<ulong, dynamic>();
 
        public BasicGetResult BasicGet(string queue, bool noAck)
        {
            models.Queue queueInstance;
            Queues.TryGetValue(queue, out queueInstance);

            if (queueInstance == null)
                return null;

            RabbitMessage message;
            queueInstance.Messages.TryDequeue(out message);

            if (message == null)
                return null;

            Interlocked.Increment(ref _lastDeliveryTag);
            var deliveryTag = Convert.ToUInt64(_lastDeliveryTag);
            const bool redelivered = false;
            var exchange = message.Exchange;
            var routingKey = message.RoutingKey;
            var messageCount = Convert.ToUInt32(queueInstance.Messages.Count);
            var basicProperties = CreateBasicProperties();
            var body = message.Body;

            Func<ulong, dynamic, dynamic> updateFunction = (key, existingMessage) => existingMessage;
            _workingMessages.AddOrUpdate(deliveryTag, message, updateFunction);

            return new BasicGetResult(deliveryTag,redelivered,exchange,routingKey,messageCount,basicProperties,body);

        }


        public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            PrefetchSize = prefetchSize;
            PrefetchCount = prefetchCount;
            ApplyPrefetchToAllChannels = global;
        }

        public void BasicPublish(PublicationAddress addr, IBasicProperties basicProperties, byte[] body)
        {
            BasicPublish(exchange: addr.ExchangeName, routingKey: addr.RoutingKey, mandatory: true, immediate: true, basicProperties: basicProperties, body: body);
        }

        public void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, byte[] body)
        {
            BasicPublish(exchange:exchange,routingKey:routingKey,mandatory:true,immediate:true,basicProperties:basicProperties,body:body);
        }

        public void BasicPublish(string exchange, string routingKey, bool mandatory, IBasicProperties basicProperties, byte[] body)
        {
            BasicPublish(exchange:exchange,routingKey:routingKey,mandatory:mandatory,immediate:true,basicProperties:basicProperties,body:body);
        }

        public void BasicPublish(string exchange, string routingKey, bool mandatory, bool immediate, IBasicProperties basicProperties,byte[] body)
        {
            var parameters = new RabbitMessage
            {
                Exchange = exchange,
                RoutingKey = routingKey,
                Mandatory = mandatory,
                Immediate = immediate,
                BasicProperties = basicProperties,
                Body = body
            };

            Func<string, Exchange> addExchange = s =>
            {
                var newExchange = new Exchange
                {
                    Name = exchange,
                    Arguments = null,
                    AutoDelete = false,
                    IsDurable = false,
                    Type = "direct"
                };
                newExchange.PublishMessage(parameters);

                return newExchange;
            };
            Func<string, Exchange, Exchange> updateExchange = (s, existingExchange) =>
            {
                existingExchange.PublishMessage(parameters);

                return existingExchange;
            };
            this.Exchanges.AddOrUpdate(exchange, addExchange, updateExchange);
        }


        public void BasicAck(ulong deliveryTag, bool multiple)
        {
            dynamic message;
            _workingMessages.TryRemove(deliveryTag, out message);
        }

        public void BasicReject(ulong deliveryTag, bool requeue)
        {
           BasicNack(deliveryTag:deliveryTag,multiple:false,requeue:requeue);
        }

        public void BasicNack(ulong deliveryTag, bool multiple, bool requeue)
        {
            dynamic message;
            _workingMessages.TryRemove(deliveryTag, out message);

            if (message != null && requeue)
            {
                models.Queue queueInstance;
                Queues.TryGetValue(message.queue, out queueInstance);

                if (queueInstance != null)
                {
                    queueInstance.Messages.Enqueue(message);
                }
            }
        }

        public void BasicRecover(bool requeue)
        {
            throw new NotImplementedException();
        }

        public void BasicRecoverAsync(bool requeue)
        {
            throw new NotImplementedException();
        }

        public void TxSelect()
        {
            throw new NotImplementedException();
        }

        public void TxCommit()
        {
            throw new NotImplementedException();
        }

        public void TxRollback()
        {
            throw new NotImplementedException();
        }

        public void DtxSelect()
        {
            throw new NotImplementedException();
        }

        public void DtxStart(string dtxIdentifier)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            IsClosed = true;
            IsOpen = false;
        }

        public void Close(ushort replyCode, string replyText)
        {
            IsClosed = true;
            IsOpen = false;
            CloseReason = new ShutdownEventArgs(ShutdownInitiator.Library, replyCode, replyText);
        }

        public void Abort()
        {
            IsClosed = true;
            IsOpen = false;
            CloseReason = null;

        }

        public void Abort(ushort replyCode, string replyText)
        {
            IsClosed = true;
            IsOpen = false;
            CloseReason = new ShutdownEventArgs(ShutdownInitiator.Library,replyCode,replyText);
        }

        public IBasicConsumer DefaultConsumer { get; set; }

        public ShutdownEventArgs CloseReason { get; set; }

        public bool IsOpen { get; set; }

        public bool IsClosed { get; set; }

        public ulong NextPublishSeqNo { get; set; }

        public event ModelShutdownEventHandler ModelShutdown;
        public event BasicReturnEventHandler BasicReturn;
        public event BasicAckEventHandler BasicAcks;
        public event BasicNackEventHandler BasicNacks;
        public event CallbackExceptionEventHandler CallbackException;
        public event FlowControlEventHandler FlowControl;
        public event BasicRecoverOkEventHandler BasicRecoverOk;
    }
}