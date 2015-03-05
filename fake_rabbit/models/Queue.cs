using System.Collections;
using System.Collections.Generic;

namespace PPA.Logging.Amqp.Tests.Fakes.models
{
    public class Queue
    {
        public string Name { get; set; }

        public bool IsDurable { get; set; }

        public bool IsExclusive { get; set; }

        public bool IsAutoDelete { get; set; }

        public IDictionary Arguments = new Dictionary<string, object>();

        public List<dynamic> Messages = new List<dynamic>();
    }
}