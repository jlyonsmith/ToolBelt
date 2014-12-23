using System;
using NUnit.Framework;
using System.Collections.Specialized;

namespace ToolBelt.Tests
{
    class MyTool : ToolBase
    {
        [CommandLineArgument("arg1")]
        public string Arg1 { get; set; }
        [CommandLineArgument("arg2")]
        [AppSettingsArgument]
        public string Arg2 { get; set; }
        [AppSettingsArgument]
        public string Arg3 { get; set; }
    }

    [TestFixture]
    public class ToolBaseTests
    {
        [Test]
        public void TestAll()
        {
            var tool = new MyTool();

            tool.ProcessAppSettings(new NameValueCollection() { { "Arg2", "789" }, { "Arg3", "#$!" } } );
            tool.ProcessCommandLine(new string[] { "-arg1:abc", "-arg2:123" });

            Assert.AreEqual("abc", tool.Arg1);
            Assert.AreEqual("123", tool.Arg2);
            Assert.AreEqual("#$!", tool.Arg3);
        }
    }
}

