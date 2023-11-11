using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AddUp.RabbitMQ.Fakes;

// Adapted from v5 Client lib
[ExcludeFromCodeCoverage]
internal sealed class QueueingBasicConsumer : DefaultBasicConsumer
{
    public sealed class SharedQueue<T> : IEnumerable<T>
    {
        private struct SharedQueueEnumerator<U> : IEnumerator<U>
        {
            private readonly SharedQueue<U> q;
            private U current;

            public U Current => current == null ? throw new InvalidOperationException() : current;
            object IEnumerator.Current => Current;

            public SharedQueueEnumerator(SharedQueue<U> queue)
            {
                q = queue;
                current = default;
            }

            public void Dispose() { /* Nothing to do here */ }

            public bool MoveNext()
            {
                try
                {
                    current = q.Dequeue();
                    return true;
                }
                catch (EndOfStreamException)
                {
                    current = default;
                    return false;
                }
            }

            public void Reset() => throw new InvalidOperationException("SharedQueue.Reset() does not make sense");
        }

        private readonly Queue<T> q = new();
        private bool isOpen = true;

        public void Close()
        {
            lock (q)
            {
                isOpen = false;
                Monitor.PulseAll(q);
            }
        }

        public T Dequeue()
        {
            lock (q)
            {
                while (q.Count == 0)
                {
                    EnsureIsOpen();
                    Monitor.Wait(q);
                }

                return q.Dequeue();
            }
        }

        public bool Dequeue(int millisecondsTimeout, out T result)
        {
            if (millisecondsTimeout == -1)
            {
                result = Dequeue();
                return true;
            }

            var now = DateTime.Now;
            lock (q)
            {
                while (q.Count == 0)
                {
                    EnsureIsOpen();
                    int elapsed = (int)(DateTime.Now - now).TotalMilliseconds;
                    int difference = millisecondsTimeout - elapsed;
                    if (difference <= 0)
                    {
                        result = default;
                        return false;
                    }

                    Monitor.Wait(q, difference);
                }

                result = q.Dequeue();
                return true;
            }
        }

        public T DequeueNoWait(T defaultValue)
        {
            lock (q)
            {
                if (q.Count == 0)
                {
                    EnsureIsOpen();
                    return defaultValue;
                }

                return q.Dequeue();
            }
        }

        public void Enqueue(T o)
        {
            lock (q)
            {
                EnsureIsOpen();
                q.Enqueue(o);
                Monitor.Pulse(q);
            }
        }

        public IEnumerator<T> GetEnumerator() => new SharedQueueEnumerator<T>(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void EnsureIsOpen()
        {
            if (!isOpen) throw new EndOfStreamException("SharedQueue closed");
        }
    }

    public QueueingBasicConsumer() : this(null) { }
    public QueueingBasicConsumer(IModel model) : this(model, new SharedQueue<BasicDeliverEventArgs>()) { }
    public QueueingBasicConsumer(IModel model, SharedQueue<BasicDeliverEventArgs> queue) : base(model) { Queue = queue; }

    public SharedQueue<BasicDeliverEventArgs> Queue { get; }

    public override void HandleBasicDeliver(
        string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, ReadOnlyMemory<byte> body) =>
        Queue.Enqueue(new BasicDeliverEventArgs
        {
            ConsumerTag = consumerTag,
            DeliveryTag = deliveryTag,
            Redelivered = redelivered,
            Exchange = exchange,
            RoutingKey = routingKey,
            BasicProperties = properties,
            Body = body
        });

    public override void OnCancel(params string[] consumerTags)
    {
        base.OnCancel(consumerTags);
        Queue.Close();
    }
}
