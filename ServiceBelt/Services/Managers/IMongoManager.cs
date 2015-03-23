using System;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Reflection;
using MongoDB.Driver.GridFS;

namespace ServiceBelt
{
    public interface IMongoManager
    {
        MongoDatabase GetDatabase();
        MongoGridFS GetGridFS(string root);
        MongoCollection<T> GetCollection<T>() where T: ICollectionObject;
        void Delete(Type collectionType, ObjectId id, Action<Type, ObjectId> deleted = null);
        void DeleteReferrers(Type collectionType, ObjectId referredToId, Action<Type, ObjectId> deleted = null);
        bool ItemExistsInCollection(Type collectionType, ObjectId id);
        Type GetReferencedCollectionType(PropertyInfo refPropInfo);
        string GetCollectionName(Type type);
        string GetCollectionName<T>();
    }
}
