using System;
using Rql;

namespace ServiceBelt
{
    public class PostResponse
    {
        public PostResponse(RqlDateTime created, RqlId id)
        {
            Created = created;
            Id = id;
        }

        public RqlDateTime Created { get; set; }

        public RqlId Id { get; set; }
    }
}

