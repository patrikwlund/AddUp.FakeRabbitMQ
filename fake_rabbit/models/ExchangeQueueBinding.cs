namespace PPA.Logging.Amqp.Tests.Fakes.models
{
    public class ExchangeQueueBinding
    {
        public string RoutingKey { get; set; }

        public Exchange Exchange { get; set; }

        public Queue Queue { get; set; }
    }
}