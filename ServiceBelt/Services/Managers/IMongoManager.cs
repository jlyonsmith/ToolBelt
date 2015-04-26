using System;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace ServiceBelt
{
    public interface IMongoManager
    {
        IMongoDatabase GetDatabase();
        IMongoCollection<T> GetCollection<T>() where T: ICollectionObject;
        Task Delete(Type collectionType, ObjectId id, Action<Type, ObjectId> deleted = null);
        Task DeleteReferrers(Type collectionType, ObjectId referredToId, Action<Type, ObjectId> deleted = null);
        Task<bool> ItemExistsInCollection(Type collectionType, ObjectId id);
        Type GetReferencedCollectionType(PropertyInfo refPropInfo);
        string GetCollectionName(Type type);
        string GetCollectionName<T>();
    }
}
