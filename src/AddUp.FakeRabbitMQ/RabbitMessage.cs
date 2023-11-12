using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes
{
    public sealed class RabbitMessage
    {
        public RabbitMessage(ulong deliveryTag) => DeliveryTag = deliveryTag;

        public string Exchange { get; set; }
        public string RoutingKey { get; set; }
        public string Queue { get; set; }
        public bool Mandatory { get; set; }
        public IBasicProperties BasicProperties { get; set; }
        public byte[] Body { get; set; }

        internal ulong DeliveryTag { get; }

        public RabbitMessage Copy() => new RabbitMessage(DeliveryTag)
        {
            Exchange = Exchange,
            RoutingKey = RoutingKey,
            Queue = Queue,
            Mandatory = Mandatory,
            BasicProperties = BasicProperties,
            Body = Body
        };
    }
}