using NUnit.Framework;
using System;
using ToolBelt;

namespace ToolBelt.Tests
{
    [TestFixture()]
    public class ParsedUrlTests
    {
        [Test()]
        public void TestSmtpAll()
        {
            ParsedUrl url = new ParsedUrl("smtp://user:password@smtp.provider.com:587");

            Assert.AreEqual("smtp", url.Scheme);
            Assert.AreEqual("smtp.provider.com", url.Host);
            Assert.AreEqual("user", url.User);
            Assert.AreEqual("password", url.Password);
            Assert.AreEqual(587, url.Port);
            Assert.AreEqual("smtp://user:password@smtp.provider.com:587", url.All);
            Assert.AreEqual("smtp://user:password@smtp.provider.com:587", url.AllNoQueryParams);
            Assert.IsNull(url.QueryParams);
        }

        [Test()]
        public void TestSmtpHostPortOnly()
        {
            ParsedUrl url = new ParsedUrl("smtp://smtp.my-provider.com:587");

            Assert.AreEqual("smtp", url.Scheme);
            Assert.AreEqual("smtp.my-provider.com", url.Host);
            Assert.IsNull(url.User);
            Assert.IsNull(url.Password);
            Assert.AreEqual(587, url.Port);
            Assert.IsNull(url.QueryParams);
        }

        [Test()]
        public void TestHttpRootPath()
        {
            ParsedUrl url = new ParsedUrl("http://abc.com/");

            Assert.AreEqual("http", url.Scheme);
            Assert.IsNull(url.User);
            Assert.IsNull(url.Password);
            Assert.AreEqual("abc.com", url.Host);
            Assert.IsNull(url.Port);
            Assert.AreEqual("/", url.Path);
            Assert.IsNull(url.QueryParams);
        }

        [Test()]
        public void TestHttpWithDeepPath()
        {
            ParsedUrl url = new ParsedUrl("http://abc.com/x/y/z");

            Assert.AreEqual("http", url.Scheme);
            Assert.IsNull(url.User);
            Assert.IsNull(url.Password);
            Assert.AreEqual("abc.com", url.Host);
            Assert.IsNull(url.Port);
            Assert.AreEqual("/x/y/z", url.Path);
            Assert.IsNull(url.QueryParams);
            Assert.AreEqual("http://abc.com/x/y/z", url.All);
            Assert.AreEqual("http://abc.com/x/y/z", url.AllNoQueryParams);
        }

        [Test()]
        public void TestHttpAndQueryParams()
        {
            ParsedUrl url = new ParsedUrl("http://abc.com/x?a&b=1&c=");

            Assert.AreEqual("http", url.Scheme);
            Assert.IsNull(url.User);
            Assert.IsNull(url.Password);
            Assert.AreEqual("abc.com", url.Host);
            Assert.IsNull(url.Port);
            Assert.AreEqual("/x", url.Path);
            Assert.AreEqual("a=&b=1&c=", url.QueryParams);
            Assert.AreEqual("http://abc.com/x?a=&b=1&c=", url.All);
            Assert.AreEqual("http://abc.com/x", url.AllNoQueryParams);
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
            Assert.IsNull(url.QueryParams);
        }

        [Test()]
        public void TestHttpPortAndQueryParam()
        {
            ParsedUrl url = new ParsedUrl("http://x.com:80/abc?a=1");

            url = url.WithPath("/");

            Assert.AreEqual("http", url.Scheme);
            Assert.AreEqual("x.com", url.Host);
            Assert.IsNull(url.User);
            Assert.IsNull(url.Password);
            Assert.AreEqual("/", url.Path);
            Assert.AreEqual(80, url.Port);
            Assert.AreEqual("a=1", url.QueryParams);
        }

        [Test()]
        public void TestWithQuery()
        {
            ParsedUrl url = new ParsedUrl("http://x.com:80/abc?a=1");

            url = url.WithQueryParams("b=2&c=3");

            Assert.AreEqual("http", url.Scheme);
            Assert.AreEqual("x.com", url.Host);
            Assert.IsNull(url.User);
            Assert.IsNull(url.Password);
            Assert.AreEqual("/abc", url.Path);
            Assert.AreEqual(80, url.Port);
            Assert.AreEqual("b=2&c=3", url.QueryParams);
        }

        [Test()]
        public void TestFile()
        {
            ParsedUrl url = new ParsedUrl("file:///Users/xxx/yyy/somefile.ext");

            Assert.AreEqual("file", url.Scheme);
            Assert.IsNull(url.Host);
            Assert.IsNull(url.User);
            Assert.IsNull(url.Password);
            Assert.AreEqual("/Users/xxx/yyy/somefile.ext", url.Path);
            Assert.IsNull(url.Port);
            Assert.IsNull(url.QueryParams);
        }

        [Test()]
        public void TestWithPort()
        {
            ParsedUrl url = new ParsedUrl("http://xyz.com/something").WithPort(80);

            Assert.AreEqual("http", url.Scheme);
            Assert.AreEqual("xyz.com", url.Host);
            Assert.IsNull(url.User);
            Assert.IsNull(url.Password);
            Assert.AreEqual("/something", url.Path);
            Assert.AreEqual(80, url.Port);
            Assert.IsNull(url.QueryParams);

            url = url.WithPort();

            Assert.IsNull(url.Port);
        }
    }
}

