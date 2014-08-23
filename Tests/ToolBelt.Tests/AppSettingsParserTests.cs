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
            [AppSettingsArgument("A number")]
            public int Number { get; set; }
            [AppSettingsArgument("A string")]
            public string String { get; set; }
            [AppSettingsArgument("A list")]
            public List<string> List { get; set; }
            [AppSettingsArgument("An enumeration")]
            public TestEnum Enum { get; set; }
            public object NotAnAppSetting { get; set; }
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

            parser.ParseAndSetTarget(collection);

            Assert.AreEqual(1234, target.Number);
            Assert.AreEqual("ABC", target.String);
            Assert.AreEqual(TestEnum.B, target.Enum);
            CollectionAssert.AreEqual(new string[] { "A", "B", "C" }, target.List);
            Assert.IsNull(target.NotAnAppSetting);
        }
    }
}

