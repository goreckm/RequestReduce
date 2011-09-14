using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RequestReduce.Reducer;

namespace RequestReduce.Module
{
    public class QueueItem
    {
        public string Urls { get; set; }
        public ResourceType Type { get; set; }
    }
}
