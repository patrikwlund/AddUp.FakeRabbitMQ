namespace RabbitMQ.Fakes.models
{
    public class ExchangeQueueBinding
    {
        public string RoutingKey { get; set; }

        public Exchange Exchange { get; set; }

        public Queue Queue { get; set; }

        public string Key
        {
            get { return string.Format("{0}|{1}|{2}", Exchange.Name, RoutingKey, Queue.Name); }
        }
    }
}