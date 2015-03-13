using System;
using Rql;

namespace ServiceBelt
{
    public class ResourceBase : IHasResourceId
    {
        public RqlId Id { get; set; }
        public RqlDateTime Updated { get; set; }
        public RqlDateTime Created { get; set; }
        public string Fields { get; set; }
    }
}

