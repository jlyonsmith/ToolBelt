using System;
using MongoDB.Bson;

namespace ServiceBelt
{
    public interface IHasObjectId
    {
        ObjectId Id { get; set; }
    }
}

