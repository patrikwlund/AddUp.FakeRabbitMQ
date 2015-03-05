using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace fake_rabbit.models
{
    public class Queue
    {
        public string Name { get; set; }

        public bool IsDurable { get; set; }

        public bool IsExclusive { get; set; }

        public bool IsAutoDelete { get; set; }

        public IDictionary Arguments = new Dictionary<string, object>();

        public ConcurrentQueue<dynamic> Messages = new ConcurrentQueue<dynamic>();
    }
}