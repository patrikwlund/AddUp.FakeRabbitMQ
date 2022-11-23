namespace AddUp.RabbitMQ.Fakes
{
    public sealed class RabbitExchangeQueueBinding
    {
        public string RoutingKey { get; set; }
        public RabbitExchange Exchange { get; set; }
        public RabbitQueue Queue { get; set; }
        public string Key => string.Format("{0}|{1}|{2}", Exchange.Name, RoutingKey, Queue.Name);
    }
}