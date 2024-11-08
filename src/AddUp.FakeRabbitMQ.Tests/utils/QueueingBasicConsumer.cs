using System.Collections;
using System.Diagnostics.CodeAnalysis;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AddUp.RabbitMQ.Fakes;

// Adapted from v5 Client lib
[ExcludeFromCodeCoverage]
internal sealed class QueueingBasicConsumer : AsyncDefaultBasicConsumer
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
    public QueueingBasicConsumer(IChannel channel) : this(channel, new SharedQueue<BasicDeliverEventArgs>()) { }
    public QueueingBasicConsumer(IChannel channel, SharedQueue<BasicDeliverEventArgs> queue) : base(channel) { Queue = queue; }

    public SharedQueue<BasicDeliverEventArgs> Queue { get; }

    public override Task HandleBasicDeliverAsync(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IReadOnlyBasicProperties properties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default)
    {
        Queue.Enqueue(new BasicDeliverEventArgs(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body.ToArray()));

        return Task.CompletedTask;
    }

    protected override async Task OnCancelAsync(string[] consumerTags, CancellationToken cancellationToken = default)
    {
        await base.OnCancelAsync(consumerTags, cancellationToken);
        Queue.Close();
    }
}
