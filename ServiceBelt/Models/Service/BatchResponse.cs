using System;
using System.Collections.Generic;

namespace ServiceBelt
{
    public class BatchResponse
    {
        public BatchResponse() 
        {
            Items = new List<object>();
        }

        public List<object> Items { get; set; }
    }
}

