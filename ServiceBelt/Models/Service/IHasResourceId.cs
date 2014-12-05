using System;
using Rql;

namespace ServiceBelt
{
    public interface IHasResourceId
    {
        RqlId Id { get; set; }
    }
}

