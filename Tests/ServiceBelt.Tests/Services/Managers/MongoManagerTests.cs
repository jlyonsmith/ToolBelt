using NUnit.Framework;
using System;
using MongoDB.Bson;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;
using System.Linq;
using System.Reflection;
using MongoDB.Driver;
using ServiceBelt;

namespace ServiceBelt.Tests
{
    public class OrderedThing : IDocumentObject
    {
        [References(typeof(Thing))]
        public ObjectId Id { get; set; }

        public int Order { get; set; }
    }

    public class HiddenThing : IDocumentObject
    {
        [References(typeof(Thing))]
        public ObjectId ThingId { get; set; }
    }

    public class ThingReferrer : ICollectionObject
    {
        public ObjectId Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

        [References(typeof(Thing))]
        public ObjectId ThingId { get; set; }

        [BsonIgnoreIfNull]
        [References(typeof(Thing))]
        public ObjectId? OptionalThingId { get; set; }

        [BsonIgnoreIfNull]
        [References(typeof(Thing))]
        public List<ObjectId> ThingIds { get; set; }

        [BsonIgnoreIfNull]
        [References(typeof(Thing))]
        public HiddenThing HiddenThing { get; set; }

        [BsonIgnoreIfNull]
        [References(typeof(Thing))]
        public List<OrderedThing> OrderedThings { get; set; } 
    }

    public class Thing : ICollectionObject
    {
        public ObjectId Id { get; set; }
        public DateTime Updated { get; set; }
        public DateTime Created { get; set; }
        public string Something { get; set; }
    }

    [TestFixture()]
    public class MongoManagerTests
    {
        [Test()]
        public async void TestDeleter()
        {
            var mongo = new MongoManager(new MongoUrl("mongodb://127.0.0.1/testMongoManager"), typeof(Thing));
            var db = mongo.GetDatabase();

            var thingCollectionName = mongo.GetCollectionName<Thing>();
            var thingReferrerCollectionName = mongo.GetCollectionName<ThingReferrer>();

            await db.DropCollectionAsync(thingCollectionName);

            var things = new List<Thing>();

            for (int i = 1; i <= 10; i++)
            {
                things.Add(new Thing { Something = String.Format("Thing{0}", i) });
            }

            await db.GetCollection<Thing>(thingCollectionName).InsertManyAsync(things);
            var thingIds = (await mongo.GetCollection<Thing>().Find(new BsonDocument()).Project(x => x.Id).ToListAsync());

            await db.DropCollectionAsync(thingReferrerCollectionName);

            var thingReferrers = new ThingReferrer[]
            {
                new ThingReferrer // 0
                {
                    ThingId = thingIds[1]     
                },
                new ThingReferrer // 1
                {
                    ThingId = thingIds[0],
                    ThingIds = new List<ObjectId> { thingIds[2], thingIds[3] }
                },
                new ThingReferrer // 2
                {
                    ThingId = thingIds[0],
                    OrderedThings = new List<OrderedThing> { new OrderedThing { Id = thingIds[4], Order = 0 }, new OrderedThing { Id = thingIds[5], Order = 1} }
                },
                new ThingReferrer // 3
                {
                    ThingId = thingIds[0],
                    HiddenThing = new HiddenThing { ThingId = thingIds[6] }
                },
                new ThingReferrer // 4
                {
                    OptionalThingId = thingIds[1],
                },
            };

            await db.GetCollection<ThingReferrer>(thingReferrerCollectionName).InsertManyAsync(thingReferrers);

            var thingReferrerIds = await mongo.GetCollection<ThingReferrer>().Find(new BsonDocument()).Project(x => x.Id).ToListAsync();

            Assert.AreEqual(5, await mongo.GetCollection<ThingReferrer>().CountAsync(new BsonDocument()));
            Assert.AreEqual(10, await mongo.GetCollection<Thing>().CountAsync(new BsonDocument()));

            ThingReferrer thingReferrer;

            await mongo.Delete(typeof(Thing), thingIds[1]);
            Assert.AreEqual(3, await mongo.GetCollection<ThingReferrer>().CountAsync(new BsonDocument()));
            Assert.AreEqual(9, await mongo.GetCollection<Thing>().CountAsync(new BsonDocument()));

            await mongo.Delete(typeof(Thing), thingIds[2]);
            Assert.AreEqual(3, await mongo.GetCollection<ThingReferrer>().CountAsync(new BsonDocument()));
            thingReferrer = await mongo.GetCollection<ThingReferrer>().Find(x => x.Id == thingReferrerIds[1]).FirstOrDefaultAsync();
            Assert.NotNull(thingReferrer);
            Assert.AreEqual(1, thingReferrer.ThingIds.Count);
            Assert.AreEqual(8, await mongo.GetCollection<Thing>().CountAsync(new BsonDocument()));
            
            await mongo.Delete(typeof(Thing), thingIds[4]);
            Assert.AreEqual(3, await mongo.GetCollection<ThingReferrer>().CountAsync(new BsonDocument()));
            thingReferrer = await mongo.GetCollection<ThingReferrer>().Find(x => x.Id == thingReferrerIds[2]).FirstOrDefaultAsync();
            Assert.NotNull(thingReferrer);
            Assert.AreEqual(1, thingReferrer.OrderedThings.Count);
            Assert.AreEqual(7, await mongo.GetCollection<Thing>().CountAsync(new BsonDocument()));

            await mongo.Delete(typeof(Thing), thingIds[6]);
            Assert.AreEqual(2, await mongo.GetCollection<ThingReferrer>().CountAsync(new BsonDocument()));
            Assert.AreEqual(6, await mongo.GetCollection<Thing>().CountAsync(new BsonDocument()));
        }
    }
}

