using System;
using System.Collections.Generic;

namespace ServiceBelt
{
    public class ListResponse<T>
    {
        public int Count { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
        public string DbQuery { get; set; }
        public List<T> Items { get; set; }
    }
}

