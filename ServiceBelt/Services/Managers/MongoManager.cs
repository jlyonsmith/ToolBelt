﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using ServiceStack.DataAnnotations;
using MongoDB.Bson;
using System.Linq;
using System.Text;
using ServiceBelt;
using MongoDB.Bson.Serialization;
using ToolBelt;
using System.Threading.Tasks;

namespace ServiceBelt
{
    public class MongoManager : IMongoManager
    {
        private enum ReferenceType
        {
            IdInDocument,
            IdInList,
            IdInDocumentList,
        }

        private class CollectionReferrer
        {
            public CollectionReferrer(Type collectionType, string fieldName, ReferenceType referenceType)
            {
                CollectionType = collectionType;
                FullFieldName = fieldName;
                ReferenceType = referenceType;
            }

            public Type CollectionType { get; set; }

            public string FullFieldName { get; set; }

            public string FieldName
            { 
                get
                {
                    int n = FullFieldName.LastIndexOf('.');
                    return FullFieldName.Substring(n == -1 ? 0 : n + 1);
                }
            }

            public string ParentFieldName
            { 
                get
                {
                    int n = FullFieldName.LastIndexOf('.');
                    return FullFieldName.Substring(0, n == -1 ? FullFieldName.Length : n);
                }
            }

            public ReferenceType ReferenceType { get; set; }
        }

        private class DeletionItem
        {
            public DeletionItem(Type collectionType, ObjectId id, bool deleteWhenUnreferred = true)
            { 
                this.CollectionType = collectionType;
                this.Id = id;
                this.DeleteWhenUnreferred = deleteWhenUnreferred;
            }

            public Type CollectionType { get; private set; }

            public ObjectId Id { get; private set; }

            public bool DeleteWhenUnreferred { get; private set; }
        }

        class ReferrerFinder
        {
            private Type referringType;
            private Type referredToType;

            public ReferrerFinder(Type referringType, Type referredToType)
            {
                this.referringType = referringType;
                this.referredToType = referredToType;
            }

            private static string MongofyFieldName(string name)
            {
                string mongoFieldName = MongoUtils.ToCamelCase(name);

                if (mongoFieldName == "id")
                {
                    // Our id fields are always munged this way
                    mongoFieldName = "_id";
                }

                return mongoFieldName;
            }

            private static string MakeFieldNames(Queue<PropertyInfo> parentPropInfos, PropertyInfo propInfo)
            {
                StringBuilder sb = new StringBuilder();

                foreach (var parentPropInfo in parentPropInfos)
                {
                    sb.Append(MongofyFieldName(parentPropInfo.Name));
                    sb.Append(".");
                }

                sb.Append(MongofyFieldName(propInfo.Name));

                return sb.ToString();
            }

            public List<CollectionReferrer> FindInType(Type type)
            {
                return InternalFindInType(type, new Queue<PropertyInfo>());
            }

            private List<CollectionReferrer> InternalFindInType(Type type, Queue<PropertyInfo> parentPropInfos)
            {
                PropertyInfo[] propInfos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                List<CollectionReferrer> referrers = new List<CollectionReferrer>();

                foreach (var propInfo in propInfos)
                {
                    if (!propInfo.CanRead)
                        continue;

                    object[] attrs = propInfo.GetCustomAttributes(typeof(ReferencesAttribute), true);

                    if (attrs.Length == 0)
                        continue;

                    var refAttr = (ReferencesAttribute)attrs[0];

                    if (refAttr.Type != referredToType)
                        continue;

                    Type propType = propInfo.PropertyType;
                    string fieldName = MakeFieldNames(parentPropInfos, propInfo);

                    if (propType == typeof(ObjectId) || propType == typeof(ObjectId?))
                    {
                        var lastPropInfo = parentPropInfos.LastOrDefault();
                        var lastPropType = lastPropInfo == null ? null : lastPropInfo.PropertyType;

                        referrers.Add(new CollectionReferrer(
                            referringType, fieldName,
                            (lastPropType != null && (lastPropType.IsGenericType && lastPropType.GetGenericTypeDefinition() == typeof(List<>))) ? 
                                ReferenceType.IdInDocumentList :
                                ReferenceType.IdInDocument));
                    }
                    else if (propType == typeof(List<ObjectId>))
                    {
                        referrers.Add(new CollectionReferrer(referringType, fieldName, ReferenceType.IdInList));
                    }
                    else if (propType.IsGenericType &&
                             propType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        Type genericArg = propType.GetGenericArguments()[0];

                        if (typeof(IDocumentObject).IsAssignableFrom(genericArg))
                        {
                            parentPropInfos.Enqueue(propInfo);
                            referrers.AddRange(InternalFindInType(genericArg, parentPropInfos));
                            parentPropInfos.Dequeue();
                        }
                    }
                    else if (typeof(IDocumentObject).IsAssignableFrom(propType))
                    {
                        parentPropInfos.Enqueue(propInfo);
                        referrers.AddRange(InternalFindInType(propType, parentPropInfos));
                        parentPropInfos.Dequeue();
                    }
                }

                return referrers;
            }
        }

        private IMongoDatabase database;
        private MongoClient client;
        private Assembly[] assemblies;
        private Dictionary<Type, List<CollectionReferrer>> typeReferrers = new Dictionary<Type, List<CollectionReferrer>>();
        private Dictionary<PropertyInfo, Type> referenceMembers = new Dictionary<PropertyInfo, Type>();
        private Queue<DeletionItem> queue = new Queue<DeletionItem>();

        public string GetCollectionName(Type type)
        {
            return MongoUtils.ToCamelCase(type.Name);
        }

        public string GetCollectionName<T>()
        {
            return GetCollectionName(typeof(T));
        }

        public MongoManager(MongoUrl mongoUrl, params Type[] dataModelMarkerTypes)
        {
            this.client = new MongoClient(mongoUrl);
            this.database = client.GetDatabase(mongoUrl.DatabaseName);
            this.assemblies = dataModelMarkerTypes.Select(t => t.Assembly).ToArray();

            var pack = new ConventionPack();

            pack.Add(new CamelCaseElementNameConvention());

            var dataModelCollectionTypes = new List<Type>();

            foreach (var dataModelMarkerType in dataModelMarkerTypes)
            {
                int n = dataModelMarkerType.FullName.LastIndexOf('.');
                string nameSpace = (n == -1 ? "" : dataModelMarkerType.FullName.Substring(0, n));

                ConventionRegistry.Register("MongoManager - " + nameSpace, pack, t => t.FullName.StartsWith(nameSpace));

                dataModelCollectionTypes.AddRange(
                    dataModelMarkerType.Assembly.GetTypes().Where(t => t.GetTypeInfo().ImplementedInterfaces.Contains(typeof(ICollectionObject))));
            }

            foreach (var dataModelCollectionType in dataModelCollectionTypes)
            {
                CreateCollectionIndexes(dataModelCollectionType);
            }

            TryRegisterSerializer(typeof(TimeSpan), new Rql.MongoDB.TimeSpanSerializer());
            TryRegisterSerializer(typeof(ParsedPath), new ParsedPathSerializer());
            TryRegisterSerializer(typeof(ParsedFilePath), new ParsedPathSerializer());
            TryRegisterSerializer(typeof(ParsedDirectoryPath), new ParsedPathSerializer());
            TryRegisterSerializer(typeof(ParsedUrl), new ParsedUrlSerializer());
            TryRegisterSerializer(typeof(ParsedEmail), new ParsedEmailSerializer());
        }

        void TryRegisterSerializer(Type type, IBsonSerializer serializer)
        {
            if (BsonSerializer.LookupSerializer(type) != null)
                return;

            BsonSerializer.RegisterSerializer(type, serializer);
        }

        async void CreateCollectionIndexes(Type dataModelType)
        {
            PropertyInfo[] propInfos = dataModelType.GetProperties();
            var collection = database.GetCollection<BsonDocument>(GetCollectionName(dataModelType));

            foreach (var propInfo in propInfos)
            {
                var attr = propInfo.GetCustomAttribute<IndexAttribute>(true);

                if (attr == null)
                    continue;

                var indexBuilder = Builders<BsonDocument>.IndexKeys;
                var index = indexBuilder.Ascending(MongoUtils.ToCamelCase(propInfo.Name));

                var options = new CreateIndexOptions 
                {
                    Unique = attr.Unique
                };

                await collection.Indexes.CreateOneAsync(index, options);
            }
        }

        public IMongoDatabase GetDatabase()
        {
            return database;
        }

        public IMongoCollection<T> GetCollection<T>() where T: ICollectionObject
        {
            return database.GetCollection<T>(GetCollectionName(typeof(T)));
        }

        private List<CollectionReferrer> GetCollectionReferrers(Type collectionType)
        {
            List<CollectionReferrer> referrers = null;

            lock (typeReferrers)
            {
                if (typeReferrers.TryGetValue(collectionType, out referrers))
                    return referrers;

                var types = new List<Type>();

                foreach (var assembly in assemblies)
                {
                    types.AddRange(assembly.GetTypes().AsEnumerable()
                        .Where(t => typeof(ICollectionObject).IsAssignableFrom(t) && t != typeof(ICollectionObject)));
                }

                referrers = new List<CollectionReferrer>();

                foreach (var type in types)
                {
                    referrers.AddRange(new ReferrerFinder(type, collectionType).FindInType(type));
                } 
            }

            return referrers;
        }

        /// <summary>>
        /// Clean-up objects that refer to an object that may or may not have been 
        /// already deleted.  This is used when scrubbing the database.
        /// </summary>
        public async Task DeleteReferrers(Type collectionType, ObjectId referredToId, Action<Type, ObjectId> deleted = null)
        {
            await InternalDelete(collectionType, referredToId, deleted, deleteWhenUnreferred: false);
        }

        /// <summary>
        /// Delete objects and remove all references from referrers, deleting them 
        /// those objects if they are no longer 
        /// </summary>
        public async Task Delete(Type collectionType, ObjectId id, Action<Type, ObjectId> deleted = null)
        {
            await InternalDelete(collectionType, id, deleted, deleteWhenUnreferred: true);
        }

        private async Task InternalDelete(Type collectionType, ObjectId id, Action<Type, ObjectId> deleted, bool deleteWhenUnreferred)
        {
            lock (queue)
            {
                queue.Enqueue(new DeletionItem(collectionType, id, deleteWhenUnreferred));
            }

            while (true)
            {
                DeletionItem item = null;

                lock (queue)
                {
                    if (queue.Count == 0)
                        break;

                    item = queue.Dequeue();
                }

                List<CollectionReferrer> referrers = GetCollectionReferrers(item.CollectionType);
                var updateBuilder = Builders<BsonDocument>.Update;
                var filterBuilder = Builders<BsonDocument>.Filter;

                // Search referrers for references to this object id
                foreach (var referrer in referrers)
                {
                    var collection = database.GetCollection<BsonDocument>(GetCollectionName(referrer.CollectionType));

                    using (var cursor = await collection.FindAsync(filterBuilder.Eq(referrer.FullFieldName, item.Id)))
                    {
                        while (await cursor.MoveNextAsync())
                        {
                            foreach (var doc in cursor.Current)
                            {
                                switch (referrer.ReferenceType)
                                {
                                case ReferenceType.IdInList:
                                    await collection.UpdateOneAsync(filterBuilder.Eq("_id", doc["_id"]), updateBuilder.Pull(referrer.FullFieldName, item.Id));
                                    break;
                                case ReferenceType.IdInDocumentList:
                                    await collection.UpdateOneAsync(filterBuilder.Eq("_id", doc["_id"]), updateBuilder.PullFilter(referrer.ParentFieldName, filterBuilder.Eq(referrer.FieldName, item.Id)));
                                    break;
                                case ReferenceType.IdInDocument:
                                    lock (queue)
                                    {
                                        queue.Enqueue(new DeletionItem(referrer.CollectionType, doc["_id"].AsObjectId, deleteWhenUnreferred: true));
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }

                // Nothing refers to this document now, we can delete it
                if (item.DeleteWhenUnreferred)
                {
                    await database.GetCollection<BsonDocument>(GetCollectionName(item.CollectionType)).DeleteOneAsync(filterBuilder.Eq("_id", item.Id));

                    if (deleted != null)
                        deleted(item.CollectionType, item.Id);
                }
            }
        }

        public async Task<bool> ItemExistsInCollection(Type collectionType, ObjectId id)
        {
            // Use ConfigureAwait when caller might block on the call
            return (await database.GetCollection<BsonDocument>(GetCollectionName(collectionType)).Find(x => x["_id"] == id).Limit(1).CountAsync().ConfigureAwait(false) != 0);
        }

        public Type GetReferencedCollectionType(PropertyInfo refPropInfo)
        {
            Type type;

            if (this.referenceMembers.TryGetValue(refPropInfo, out type))
            {
                return type;
            }

            object[] attrs = refPropInfo.GetCustomAttributes(typeof(ReferencesAttribute), true);

            if (attrs.Length > 0)
            {
                var refAttr = (ReferencesAttribute)attrs[0];

                type = refAttr.Type;
                referenceMembers[refPropInfo] = type;
                return type;
            }

            return null;
        }
    }
}

