using RabbitMQ.Client;

namespace AddUp.RabbitMQ.Fakes
{
    internal sealed class RabbitMessage
    {
        public string Exchange { get; set; }
        public string RoutingKey { get; set; }
        public string Queue { get; set; }
        public bool Mandatory { get; set; }
        public IBasicProperties BasicProperties { get; set; }
        public byte[] Body { get; set; }

        public RabbitMessage Copy() => new RabbitMessage
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