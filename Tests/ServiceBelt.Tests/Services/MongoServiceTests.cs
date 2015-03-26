using System;
using NUnit.Framework;
using Rql;
using MongoDB.Bson;
using MongoDB.Driver;
using ServiceStack;
using ServiceStack.FluentValidation;
using Funq;
using ServiceStack.Web;
using System.Net;

namespace ServiceBelt.Tests
{
    class SmoData : ResourceBase
    {
        public string Field1 { get; set; }
        public string Field2 { get; set; }
        public string Field3 { get; set; }
    }

    class SmoQuery : ResourceGetParams
    {
    }

    class DmoData : ICollectionObject
    {
        public ObjectId Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string Field1 { get; set; }
        public string Field2 { get; set; }
        public string Field3 { get; set; }
    }

    class DmoDataValidator : AbstractValidator<DmoData>
    {
        public DmoDataValidator()
        {
            RuleFor(x => x.Field1).NotNull();
            RuleFor(x => x.Field2).NotNull();
            RuleFor(x => x.Field3).NotNull();
        }
    }

    [TestFixture]
    public class MongoServiceTests
    {
        [Test]
        public void TestPostGetPutDelete()
        {
            var mongo = new MongoManager(new MongoUrl("mongodb://127.0.0.1/testMongoService"), typeof(DmoData));
            var container = new Funq.Container();

            container.Register<IMongoManager>(mongo);
            container.RegisterAutoWiredAs<DmoDataValidator, IValidator<DmoData>>();

            var service = new MongoService<SmoData, SmoQuery, DmoData>(container)
            {
                Mongo = mongo
            };

            RqlHelper.AddRqlPropertyCopiers();

            mongo.GetDatabase().DropCollection("test");

            var postResult = service.Post(new SmoData
                {
                    Field1 = "a",
                    Field2 = "b",
                    Field3 = "c"
                });

            Assert.AreEqual((int)HttpStatusCode.Created, postResult.Status);

            var id = ((PostResponse)postResult.Response).Id;

            var getResult = service.Get(new SmoQuery { Id = id }) as SmoData;

            Assert.NotNull(getResult);
            Assert.AreEqual("a", getResult.Field1);
            Assert.AreEqual("b", getResult.Field2);
            Assert.AreEqual("c", getResult.Field3);

            var putResult = service.Put(new SmoData { Id = id, Field1 = "x", Field2 = "y", Field3 = "z" });

            Assert.Less((DateTime)putResult.Updated, DateTime.UtcNow);

            getResult = service.Get(new SmoQuery { Id = ((PostResponse)postResult.Response).Id }) as SmoData;

            Assert.NotNull(getResult);
            Assert.AreEqual("x", getResult.Field1);
            Assert.AreEqual("y", getResult.Field2);
            Assert.AreEqual("z", getResult.Field3);

            putResult = service.Put(new SmoData { Id = id, Field1 = "X", Fields = "field1(1)" });

            getResult = service.Get(new SmoQuery { Id = id }) as SmoData;

            Assert.NotNull(getResult);
            Assert.AreEqual("X", getResult.Field1);
            Assert.AreEqual("y", getResult.Field2);
            Assert.AreEqual("z", getResult.Field3);

            service.Delete(new SmoQuery { Id = id });

            Assert.Throws<HttpError>(() => service.Get(new SmoQuery { Id = id }));
        }
    }
}

