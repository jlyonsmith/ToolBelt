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
        void Delete(Type collectionType, ObjectId id);
        void CleanUpAlreadyDeleted(Type collectionType, ObjectId id);
        bool ItemExistsInCollection(Type collectionType, ObjectId id);
        Type GetReferencedCollectionType(PropertyInfo refPropInfo);
    }
}
