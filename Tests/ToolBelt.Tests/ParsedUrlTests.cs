using NUnit.Framework;
using System;
using ToolBelt;

namespace ToolBelt.Tests
{
    [TestFixture()]
    public class ParsedUrlTests
    {
        [Test()]
        public void TestAll()
        {
            ParsedUrl url = new ParsedUrl("smtp://user:password@smtp.provider.com:587");

            Assert.AreEqual("smtp", url.Scheme);
            Assert.AreEqual("smtp.provider.com", url.Host);
            Assert.AreEqual("user", url.User);
            Assert.AreEqual("password", url.Password);
            Assert.AreEqual(587, url.Port);
            Assert.IsNull(url.Query);
        }

        [Test()]
        public void TestHostPortOnly()
        {
            ParsedUrl url = new ParsedUrl("smtp://smtp.my-provider.com:587");

            Assert.AreEqual("smtp", url.Scheme);
            Assert.AreEqual("smtp.my-provider.com", url.Host);
            Assert.IsNull(url.User);
            Assert.IsNull(url.Password);
            Assert.AreEqual(587, url.Port);
            Assert.IsNull(url.Query);
        }

        [Test()]
        public void TestRootPath()
        {
            ParsedUrl url = new ParsedUrl("http://abc.com/");

            Assert.AreEqual("http", url.Scheme);
            Assert.IsNull(url.User);
            Assert.IsNull(url.Password);
            Assert.AreEqual("abc.com", url.Host);
            Assert.IsNull(url.Port);
            Assert.AreEqual("/", url.Path);
            Assert.IsNull(url.Query);
        }

        [Test()]
        public void TestDeepPath()
        {
            ParsedUrl url = new ParsedUrl("http://abc.com/x/y/z");

            Assert.AreEqual("http", url.Scheme);
            Assert.IsNull(url.User);
            Assert.IsNull(url.Password);
            Assert.AreEqual("abc.com", url.Host);
            Assert.IsNull(url.Port);
            Assert.AreEqual("/x/y/z", url.Path);
            Assert.IsNull(url.Query);
        }

        [Test()]
        public void TestParams()
        {
            ParsedUrl url = new ParsedUrl("http://abc.com/x?a&b=1&c=");

            Assert.AreEqual("http", url.Scheme);
            Assert.IsNull(url.User);
            Assert.IsNull(url.Password);
            Assert.AreEqual("abc.com", url.Host);
            Assert.IsNull(url.Port);
            Assert.AreEqual("/x", url.Path);
            Assert.AreEqual("a=&b=1&c=", url.Query);
        }

        [Test()]
        public void TestWildcardHostOnly()
        {
            ParsedUrl url = new ParsedUrl("http://*:80");

            Assert.AreEqual("http", url.Scheme);
            Assert.AreEqual("*", url.Host);
            Assert.IsNull(url.User);
            Assert.IsNull(url.Password);
            Assert.IsNull(url.Path);
            Assert.AreEqual(80, url.Port);
            Assert.IsNull(url.Query);
        }

        [Test()]
        public void TestWithPath()
        {
            ParsedUrl url = new ParsedUrl("http://x.com:80/abc?a=1");

            url = url.WithPath("/");

            Assert.AreEqual("http", url.Scheme);
            Assert.AreEqual("x.com", url.Host);
            Assert.IsNull(url.User);
            Assert.IsNull(url.Password);
            Assert.AreEqual("/", url.Path);
            Assert.AreEqual(80, url.Port);
            Assert.AreEqual("a=1", url.Query);
        }

        [Test()]
        public void TestWithQuery()
        {
            ParsedUrl url = new ParsedUrl("http://x.com:80/abc?a=1");

            url = url.WithQuery("?b=2&c=3");

            Assert.AreEqual("http", url.Scheme);
            Assert.AreEqual("x.com", url.Host);
            Assert.IsNull(url.User);
            Assert.IsNull(url.Password);
            Assert.AreEqual("/abc", url.Path);
            Assert.AreEqual(80, url.Port);
            Assert.AreEqual("b=2&c=3", url.Query);
        }
        
        [Test()]
        public void TestWithFile()
        {
            ParsedUrl url = new ParsedUrl("file:///Users/xxx/yyy/somefile.ext");

            Assert.AreEqual("file", url.Scheme);
            Assert.IsNull(url.Host);
            Assert.IsNull(url.User);
            Assert.IsNull(url.Password);
            Assert.AreEqual("/Users/xxx/yyy/somefile.ext", url.Path);
            Assert.IsNull(url.Port);
            Assert.IsNull(url.Query);
        }
    }
}

