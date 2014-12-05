using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using ServiceStack.DataAnnotations;
using MongoDB.Bson;
using System.Linq;
using System.Text;
using ServiceBelt;
using MongoDB.Driver.GridFS;
using MongoDB.Bson.Serialization;

namespace ServiceBelt
{
    public class MongoManager : IMongoManager
    {
        private enum ReferenceType
        {
            Id,
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

                    if (propType == typeof(ObjectId))
                    {
                        var lastPropInfo = parentPropInfos.LastOrDefault();
                        var lastPropType = lastPropInfo == null ? null : lastPropInfo.PropertyType;

                        referrers.Add(new CollectionReferrer(
                            referringType, fieldName,
                            lastPropType == null ? ReferenceType.Id :
                            (lastPropType.IsGenericType && lastPropType.GetGenericTypeDefinition() == typeof(List<>)) ? ReferenceType.IdInDocumentList :
                            typeof(IDocumentObject).IsAssignableFrom(lastPropType) ? ReferenceType.IdInDocument : 
                            ReferenceType.Id));
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

        private MongoDatabase database;
        private MongoServer server;
        private MongoClient client;
        private Assembly[] assemblies;
        private Dictionary<Type, List<CollectionReferrer>> typeReferrers = new Dictionary<Type, List<CollectionReferrer>>();
        private Dictionary<PropertyInfo, Type> referenceMembers = new Dictionary<PropertyInfo, Type>();
        private Queue<DeletionItem> queue = new Queue<DeletionItem>();

        private string GetCollectionNameFromType(Type type)
        {
            return MongoUtils.ToCamelCase(type.Name);
        }

        public MongoManager(MongoUrl mongoUrl, params Type[] dataModelMarkerTypes)
        {
            this.client = new MongoClient(mongoUrl);
            this.server = client.GetServer();
            this.database = server.GetDatabase(mongoUrl.DatabaseName);
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

            BsonSerializer.RegisterSerializer(typeof(TimeSpan), new Rql.MongoDB.TimeSpanSerializer());
        }

        void CreateCollectionIndexes(Type dataModelType)
        {
            PropertyInfo[] propInfos = dataModelType.GetProperties();
            var collection = database.GetCollection(GetCollectionNameFromType(dataModelType));

            foreach (var propInfo in propInfos)
            {
                object[] attrs = propInfo.GetCustomAttributes(typeof(IndexAttribute), true);

                if (attrs.Length != 1)
                    continue;

                var attr = (IndexAttribute)attrs[0];

                IndexKeysBuilder index = new IndexKeysBuilder();

                index.Ascending(MongoUtils.ToCamelCase(propInfo.Name));

                IndexOptionsBuilder options = new IndexOptionsBuilder();

                options.SetUnique(attr.Unique);

                collection.CreateIndex(index);
            }
        }

        public MongoDatabase GetDatabase()
        {
            return database;
        }

        // BUG #76: Change this to take an enumeration of the possible roots; slide, image
        public MongoGridFS GetGridFS(string root)
        {
            var settings = new MongoGridFSSettings()
            {
                Root = root,
                ChunkSize = 31 * 1024
            };

            return GetDatabase().GetGridFS(settings);
        }

        public MongoCollection<T> GetCollection<T>() where T: ICollectionObject
        {
            return database.GetCollection<T>(GetCollectionNameFromType(typeof(T)));
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

        public void CleanUpAlreadyDeleted(Type collectionType, ObjectId id)
        {
            InternalDelete(collectionType, id, deleteWhenUnreferred: false);
        }

        public void Delete(Type collectionType, ObjectId id)
        {
            InternalDelete(collectionType, id, deleteWhenUnreferred: true);
        }

        public void InternalDelete(Type collectionType, ObjectId id, bool deleteWhenUnreferred)
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

                // Search referrers for references to this object id
                foreach (var referrer in referrers)
                {
                    MongoCollection coll = database.GetCollection(GetCollectionNameFromType(referrer.CollectionType));

                    var cursor = coll.FindAs<BsonDocument>(Query.EQ(referrer.FullFieldName, item.Id));

                    foreach (var doc in cursor)
                    {
                        if (referrer.ReferenceType == ReferenceType.IdInList)
                        {
                            coll.Update(Query.EQ("_id", doc["_id"]), Update.Pull(referrer.FullFieldName, item.Id));
                        }
                        else if (referrer.ReferenceType == ReferenceType.IdInDocumentList)
                        {
                            coll.Update(Query.EQ("_id", doc["_id"]), Update.Pull(referrer.ParentFieldName, Query.EQ(referrer.FieldName, item.Id)));
                        }
                        else
                        {
                            lock (queue)
                            {
                                queue.Enqueue(new DeletionItem(referrer.CollectionType, doc["_id"].AsObjectId, deleteWhenUnreferred: true));
                            }
                        }
                    }
                }

                if (item.DeleteWhenUnreferred)
                    database.GetCollection(GetCollectionNameFromType(item.CollectionType)).Remove(Query.EQ("_id", item.Id));
            }
        }

        public bool ItemExistsInCollection(Type collectionType, ObjectId id)
        {
            return (database.GetCollection(GetCollectionNameFromType(collectionType)).Find(Query.EQ("_id", id)).SetLimit(1).Size() != 0);
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

