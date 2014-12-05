using System;
using Rql;

namespace ServiceBelt
{
    public abstract class ResourceGetParams
    {
        public ResourceGetParams()
        {
            this.Id = null;
            this.Limit = 10;
            this.Offset = 0;
            this.Where = null;
            this.Sort = null;
            this.Fields = null;
        }

        public RqlId? Id { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
        public string Where { get; set; }
        public string Sort { get; set; }
        public string Fields { get; set; }
    }
}

