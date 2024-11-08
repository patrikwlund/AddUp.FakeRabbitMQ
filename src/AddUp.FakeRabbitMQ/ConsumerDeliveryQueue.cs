using System.Threading.Channels;
using RabbitMQ.Client.Events;

namespace AddUp.RabbitMQ.Fakes
{
    internal abstract class ConsumerDeliveryQueue
    {
        private readonly Action<CallbackExceptionEventArgs> onDeliveryException;
        protected readonly FakeModel Model;

        protected ConsumerDeliveryQueue(FakeModel model, Action<CallbackExceptionEventArgs> deliveryExceptionHandler)
        {
            Model = model;
            onDeliveryException = deliveryExceptionHandler;
        }

        // Factory method returning either a blocking or a non-blocking implementation
        public static ConsumerDeliveryQueue Create(
            FakeModel model,
            Action<CallbackExceptionEventArgs> deliveryExceptionHandler,
            bool createBlockingDeliveryQueue) =>
                createBlockingDeliveryQueue
                ? new BlockingDeliveryQueue(model, deliveryExceptionHandler)
                : new NonBlockingDeliveryQueue(model, deliveryExceptionHandler);

        public abstract Task Deliver(Func<CancellationToken, Task> deliveryAction, CancellationToken cancellationToken);

        /// <summary>
        /// Marks the queue as complete, meaning no more new deliveries will be accepted.
        /// </summary>
        public abstract void Complete();

        /// <summary>
        /// Wait for any remaining queues deliveries to finish.
        /// </summary>
        public abstract void WaitForCompletion();

        protected async Task ExecuteDelivery(Func<CancellationToken, Task> deliveryAction, CancellationToken cancellationToken)
        {
            try
            {
                if (!Model.IsOpen) return;
                await deliveryAction(cancellationToken);
            }
            catch (Exception ex)
            {
                var callbackArgs = CallbackExceptionEventArgs.Build(ex, "");
                onDeliveryException(callbackArgs);
            }
        }
    }

    internal sealed class NonBlockingDeliveryQueue : ConsumerDeliveryQueue
    {
        private readonly Task deliveriesTask;
        private readonly AsyncLocal<bool> isDeliveriesTask = new AsyncLocal<bool>();
        private readonly Channel<Func<CancellationToken, Task>> deliveries = Channel.CreateUnbounded<Func<CancellationToken, Task>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

        public NonBlockingDeliveryQueue(FakeModel model, Action<CallbackExceptionEventArgs> onDeliveryException)
            : base(model, onDeliveryException) =>
            deliveriesTask = Task.Factory.StartNew(HandleDeliveries, TaskCreationOptions.LongRunning);

        public override async Task Deliver(Func<CancellationToken, Task> deliveryAction, CancellationToken cancellationToken)
        {
            await deliveries.Writer.WriteAsync(deliveryAction, cancellationToken);
        }

        /// <summary>
        /// Rabbit docs states that each connection is backed by a single background thread:
        /// 
        /// https://www.rabbitmq.com/dotnet-api-guide.html#concurrency-thread-usage
        /// 
        /// However, this is not actually true, it's backed by a Task:
        /// 
        /// https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/65dd5f92dda130ec35b4ad6fe7bc54dbcb1637fd/projects/RabbitMQ.Client/client/impl/ConsumerWorkService.cs#L81
        /// 
        /// FakeModels aren't aware of their connection, so in order to emulate this, just
        /// run a task that handles deliveries per task. It's necessary to match RabbitMQ
        /// semantics as running delivery callbacks synchronously can cause deadlocks in
        /// code under test.
        /// </summary>
        private async Task HandleDeliveries()
        {
            try
            {
                isDeliveriesTask.Value = true;
                while (await deliveries.Reader.WaitToReadAsync().ConfigureAwait(false))
                {
                    while (deliveries.Reader.TryRead(out var delivery))
                    {
                        await ExecuteDelivery(delivery, CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                // Swallow exceptions so FakeModel.Close() doesn't have to deal with it.

                await Console.Error.WriteLineAsync("An error occurred while handling deliveries: " + ex);
            }
        }

        public override void Complete() =>
            _ = deliveries.Writer.TryComplete();

        public override void WaitForCompletion()
        {
            // It's possible that we can end up calling Close on a model from within the delivery handler.
            // If this is the case, we must not wait on it to complete as this will deadlock!
            if (!isDeliveriesTask.Value)
                deliveriesTask.Wait();
        }
    }

    internal sealed class BlockingDeliveryQueue : ConsumerDeliveryQueue
    {
        private static readonly TimeSpan DeliveryWaitTimeout = TimeSpan.FromMinutes(1);
        private readonly SemaphoreSlim deliveryLock = new SemaphoreSlim(1);
        private volatile bool notAcceptingNewDeliveries = false;

        public BlockingDeliveryQueue(FakeModel model, Action<CallbackExceptionEventArgs> onDeliveryException)
            : base(model, onDeliveryException) { }

        public override async Task Deliver(Func<CancellationToken, Task> deliveryAction, CancellationToken cancellationToken)
        {
            if (notAcceptingNewDeliveries)
                return;

            try
            {
                var allowed = await deliveryLock.WaitAsync(DeliveryWaitTimeout);
                if (!allowed)
                {
                    throw new TimeoutException("Timeout waiting for delivery lock");
                }

                await ExecuteDelivery(deliveryAction, cancellationToken);
            }
            finally
            {
                _ = deliveryLock.Release();
            }
        }

        public override void Complete() => notAcceptingNewDeliveries = true;

        public override void WaitForCompletion()
        {
            try
            {
                _ = deliveryLock.Wait(DeliveryWaitTimeout);
            }
            finally
            {
                _ = deliveryLock.Release();
            }
        }
    }
}
