using System;
using NUnit.Framework;
using System.Collections.Generic;
using ToolBelt_ServiceStack;

namespace Property
{
    [TestFixture()]
    public class PropertyCopierTests
    {
        enum MyEnum
        {
            First,
            Second,
            Third
        }

        class ServiceThing
        {
            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType()) 
                    return false;
                ServiceThing p = (ServiceThing)obj;
                return (Name == p.Name) && (Value == p.Value);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public string Name { get; set; }
            public string Value { get; set; }
            public string NotInData { get { return "Dynamic!"; } }
        }

        class ServiceClass
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public int Length { get; set; }
            public int? Nullable1 { get; set; }
            public int? Nullable2 { get; set; }
            public double Pi { get; set; }
            public DateTime When { get; set; }
            public TimeSpan AnotherWhen { get; set; }
            public Guid? MaybeId { get; set; }
            public ServiceThing Thing { get; set; }
            public List<string> Stuff { get; set; }
            public List<Guid> Links { get; set; }
            public List<ServiceThing> Things { get; set; }
            public Dictionary<string, string> Mapping { get; set; }
            public string Contents { get; set; }
            public string Enum { get; set; }
            public List<string> Enums { get; set; }
            public int HiddenEnum { get; set; }
            public List<int> HiddenEnums { get; set; }
        }

        class DataThing
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        class DataClass
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public int Length { get; set; }
            public int? Nullable1 { get; set; }
            public int? Nullable2 { get; set; }
            public double Pi { get; set; }
            public DateTime When { get; set; }
            public TimeSpan AnotherWhen { get; set; }
            public Guid? MaybeId { get; set; }
            public DataThing Thing { get; set; }
            public List<string> Stuff { get; set; }
            public List<Guid> Links { get; set; }
            public List<DataThing> Things { get; set; }
            public Dictionary<string, string> Mapping { get; set; }
            public object SomeId { get; set; }
            public string Contents { get; set; }
            public MyEnum Enum { get; set; }
            public List<MyEnum> Enums { get; set; }
            public MyEnum HiddenEnum { get; set; }
            public List<MyEnum> HiddenEnums { get; set; }
        }

        [Test()]
        public void TestModelMapper()
        {
            var service1 = new ServiceClass()
            { 
                Id = new Guid("51438b2f-4cd7-4e02-0000-000100000000"),
                Name = "John",
                Length = 1024,
                Nullable1 = 10,
                Nullable2 = null,
                Pi = 3.14,
                When = DateTime.SpecifyKind(new DateTime(2013, 06, 24, 8, 0, 0), DateTimeKind.Utc),
                AnotherWhen = new TimeSpan(6, 12, 8, 0),
                MaybeId = new Guid("51438b2f-4cd7-4e02-0000-033300000000"),
                Thing = new ServiceThing { Name = "X", Value = "123" },
                Stuff = new List<string> { "A", "B", "C" },
                Links = new List<Guid> { new Guid("51438b2f-4cd7-ff02-0000-000100000000"), new Guid("51438b2f-4cd7-ff02-0000-000200000000") },
                Things = new List<ServiceThing> { new ServiceThing { Name = "Blue", Value="0000FF" }  },
                Mapping = new Dictionary<string, string> 
                {
                    { "A", "1" },
                    { "B", "2" },
                    { "C", "2" }
                },
                Contents = "{ \"list\" : [\"1\", \"2\", \"3\"], \"anotherList\" : [\"A\", \"B\", \"C\"] }",
                Enum = "First",
                Enums = new List<string> { "First", "Second" },
                HiddenEnum = 2,
                HiddenEnums = new List<int> { 1, 2 }
            };

            var data = service1.CopyAsNew<DataClass>();

            var service2 = data.CopyAsNew<ServiceClass>();

            Assert.AreEqual(service1.Id, service2.Id);
            Assert.AreEqual(service1.Name, service2.Name);
            Assert.AreEqual(service1.Length, service2.Length);
            Assert.AreEqual(service1.Nullable1, service2.Nullable1);
            Assert.AreEqual(service1.Nullable2, service2.Nullable2);
            Assert.AreEqual(service1.Pi, service2.Pi);
            Assert.AreEqual(service1.When, service2.When);
            Assert.AreEqual(service1.AnotherWhen, service2.AnotherWhen);
            Assert.AreEqual(service1.MaybeId, service2.MaybeId);
            CollectionAssert.AreEqual(service1.Stuff, service2.Stuff);
            CollectionAssert.AreEqual(service1.Links, service2.Links);
            CollectionAssert.AreEqual(service1.Things, service2.Things);
            CollectionAssert.AreEqual(service1.Mapping, service2.Mapping);
            Assert.AreEqual(service1.Contents, service2.Contents);
            Assert.AreEqual(service1.Enum, service2.Enum);
            CollectionAssert.AreEqual(service1.Enums, service2.Enums);
        }
    }
}

