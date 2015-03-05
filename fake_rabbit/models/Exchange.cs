using System.Collections;
using System.Collections.Generic;

namespace PPA.Logging.Amqp.Tests.Fakes.models
{
    public class Exchange
    {
        public string Name { get; set; } 
        public string Type { get; set; } 
        public bool IsDurable { get; set; } 
        public bool AutoDelete { get; set; } 
        public IDictionary Arguments = new Dictionary<string, object>();

        public List<dynamic> PublishedMessages = new List<dynamic>();
    }
}