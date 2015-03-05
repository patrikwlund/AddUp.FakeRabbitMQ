using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace fake_rabbit.models
{
    public class Exchange
    {
        public string Name { get; set; } 
        public string Type { get; set; } 
        public bool IsDurable { get; set; } 
        public bool AutoDelete { get; set; } 
        public IDictionary Arguments = new Dictionary<string, object>();

        public ConcurrentQueue<dynamic> Messages = new ConcurrentQueue<dynamic>();
        public ConcurrentDictionary<string,ExchangeQueueBinding> Bindings = new ConcurrentDictionary<string,ExchangeQueueBinding>(); 
    }
}