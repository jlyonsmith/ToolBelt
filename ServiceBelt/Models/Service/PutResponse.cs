using System;
using Rql;

namespace ServiceBelt
{
    public class PutResponse
    {
        public PutResponse(RqlDateTime updated)
        {
            Updated = updated;
        }

        public RqlDateTime Updated { get; set; }
    }
}

