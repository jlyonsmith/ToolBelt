using System;
using NUnit.Framework;

namespace ToolBelt.Tests
{
    [TestFixture()]
    public class ParsedEmailTests
    {
        [Test()]
        public void TestAll()
        {
            ParsedEmail mail = new ParsedEmail("mail://admin@google.com");

            Assert.AreEqual("mail", mail.Scheme);
            Assert.AreEqual("google.com", mail.Host);
            Assert.AreEqual("admin", mail.User);
        }

        [Test()]
        public void TestUserHostOnly()
        {
            ParsedEmail mail = new ParsedEmail("johnny.be-good@be-good-be-good.org");

            Assert.AreEqual("mail", mail.Scheme);
            Assert.AreEqual("johnny.be-good", mail.User);
            Assert.AreEqual("be-good-be-good.org", mail.Host);
        }
    }
}

