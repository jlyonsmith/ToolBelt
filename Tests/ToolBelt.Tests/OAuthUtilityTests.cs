using System;
using NUnit.Framework;
using ToolBelt;
using System.Net.Http;

namespace ToolBelt.Tests
{
    [TestFixture]
    public class OAuthUtilityTests
    {
        [Test]
        public void TestAll()
        {
            var url = new ParsedUrl("http://somwhere.com/something?a=123&b=$@~");
            string consumerKey="123467890";
            string consumerSecret="ChristmasComesButOnceAYear";
            string oAuthToken = "ABCDEFGHIJK";
            string oAuthTokenSecret = "ARoseByAnyOtherNameWouldSmellAsSweet";

            var authHeader = OAuthUtility.GenerateAuthHeader(
                url, consumerKey, consumerSecret, oAuthToken, oAuthTokenSecret, HttpMethod.Get, "123456", "123456");

            Assert.AreEqual("OAuth oauth_consumer_key=\"123467890\",oauth_token=\"ABCDEFGHIJK\",oauth_signature=\"YbW%2FXsW2MvbK2Kdb%2BReaQ2GpM%2B0%3D\",oauth_signature_method=\"HMAC-SHA1\",oauth_timestamp=\"123456\",oauth_nonce=\"123456\",oauth_version=\"1.0\"", authHeader);
        }
    }
}

