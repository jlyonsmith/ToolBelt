using System;
using MongoDB.Bson;

namespace ServiceBelt
{
    public interface ICollectionObject : IHasObjectId, IDocumentObject
    {
        DateTime Created { get; set; }
        DateTime Updated { get; set; }
    }
}

