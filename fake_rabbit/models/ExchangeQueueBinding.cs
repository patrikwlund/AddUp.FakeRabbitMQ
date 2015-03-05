namespace fake_rabbit.models
{
    public class ExchangeQueueBinding
    {
        public string RoutingKey { get; set; }

        public Exchange Exchange { get; set; }

        public Queue Queue { get; set; }
    }
}