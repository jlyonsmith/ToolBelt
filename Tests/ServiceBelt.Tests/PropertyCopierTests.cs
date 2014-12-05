using NUnit.Framework;
using System;
using MongoDB.Bson;
using ServiceBelt;
using System.Collections.Generic;

namespace ServiceBelt
{
    [TestFixture()]
    public class PropertyCopierTests
    {
        struct MyId
        {
            public string Id { get; set; }

            public MyId(ObjectId id) : this()
            {
                Id = id.ToString();
            }

            public static MyId GenerateNewId()
            {
                return new MyId(ObjectId.GenerateNewId());
            }
        }

        class DataClass
        {
            public ObjectId Id { get; set; }
            public ObjectId[] Ids { get; set; }
            public List<ObjectId> MoreIds { get; set; }
        }

        class ServiceClass
        {
            public MyId Id { get; set; }
            public MyId[] Ids { get; set; }
            public List<MyId> MoreIds { get; set; }
        }

        [Test()]
        public void TestCustomTypeCopying()
        {
            PropertyCopier.ClearTypeConverters();
            PropertyCopier.AddTypeConverter<MyId, ObjectId>(fromValue => new ObjectId(((MyId)fromValue).Id));
            PropertyCopier.AddTypeConverter<ObjectId, MyId>(fromValue => new MyId((ObjectId)fromValue));

            ServiceClass service1 = new ServiceClass()
            {
                Id = MyId.GenerateNewId(),
                Ids = new MyId[] { MyId.GenerateNewId(), MyId.GenerateNewId() },
                MoreIds = new List<MyId> { MyId.GenerateNewId(), MyId.GenerateNewId() }
            };

            var data = service1.CopyAsNew<DataClass>();

            var service2 = data.CopyAsNew<ServiceClass>();

            Assert.AreEqual(service1.Id, service2.Id);
            CollectionAssert.AreEqual(service1.Ids, service2.Ids);
            CollectionAssert.AreEqual(service1.MoreIds, service2.MoreIds);
        }
    }
}

