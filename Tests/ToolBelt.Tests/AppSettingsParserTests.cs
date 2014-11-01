using NUnit.Framework;
using System;
using System.Collections.Generic;
using ToolBelt;
using System.Collections.Specialized;

namespace ToolBelt.Tests
{
    [TestFixture()]
    public class AppSettingsParserTests
    {
        enum TestEnum
        {
            A = 0,
            B = 1,
            C = 2
        }

        class BasicAppSettings
        {
            [AppSettingsArgument(Description = "A number")]
            public int Number { get; set; }
            [AppSettingsArgument(Description = "A string")]
            public string String { get; set; }
            [AppSettingsArgument()]
            public List<string> List { get; set; }
            [AppSettingsArgument()]
            public List<Uri> Uris { get; set; }
            [AppSettingsArgument(Description = null)]
            public TestEnum Enum { get; set; }
            public object NotAnAppSetting { get; set; }
            [AppSettingsArgument(Initializer=typeof(CustomTypeInitializer), MethodName="Parse")]
            public CustomType Custom { get; set; }
            [AppSettingsArgument(Initializer=typeof(BasicAppSettings), MethodName="Parse")]
            public string Custom2 { get; set; }
            [AppSettingsArgument]
            public bool Flag { get; set; }

            public static string Parse(string arg)
            {
                return arg + "xxx";
            }
        }

        [Test()]
        public void TestCase()
        {
            var target = new BasicAppSettings();
            AppSettingsParser parser = new AppSettingsParser(target);

            var collection = new NameValueCollection();

            collection.Add("Number", "1234");
            collection.Add("String", "ABC");
            collection.Add("Enum", "B");
            collection.Add("List", "A,B,C");
            collection.Add("Uris", "http://here.com,http://there.com");
            collection.Add("Custom", "a=1;b=2");
            collection.Add("Custom2", "a");
            collection.Add("Flag", "false");

            parser.ParseAndSetTarget(collection);

            Assert.AreEqual(1234, target.Number);
            Assert.AreEqual("ABC", target.String);
            Assert.AreEqual(TestEnum.B, target.Enum);
            CollectionAssert.AreEqual(new string[] { "A", "B", "C" }, target.List);
            CollectionAssert.AreEqual(new Uri[] { new Uri("http://here.com"), new Uri("http://there.com") }, target.Uris);
            Assert.IsNull(target.NotAnAppSetting);
            CollectionAssert.AreEquivalent(new KeyValuePair<string, string>[] 
                { new KeyValuePair<string, string>("a", "1"), new KeyValuePair<string, string>("b", "2") }, target.Custom.Parameters);
            Assert.AreEqual("axxx", target.Custom2);
            Assert.AreEqual(false, target.Flag);
        }
    }
}

