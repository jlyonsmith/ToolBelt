using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Net.Http;
using System.Linq;

namespace ToolBelt
{
    // See http://oauth.net/core/1.0a for more information
    //
    public static class OAuthUtility
    {
        public static readonly string OAuthVersion = "1.0";
        public static readonly string OAuthParameterPrefix = "oauth_";
        public static readonly string OAuthConsumerKeyKey = "oauth_consumer_key";
        public static readonly string OAuthCallbackKey = "oauth_callback";
        public static readonly string OAuthVersionKey = "oauth_version";
        public static readonly string OAuthSignatureMethodKey = "oauth_signature_method";
        public static readonly string OAuthSignatureKey = "oauth_signature";
        public static readonly string OAuthTimestampKey = "oauth_timestamp";
        public static readonly string OAuthNonceKey = "oauth_nonce";
        public static readonly string OAuthTokenKey = "oauth_token";
        public static readonly string OAuthTokenSecretKey = "oauth_token_secret";
        public static readonly string HMACSHA1SignatureType = "HMAC-SHA1";
        public static readonly string PlainTextSignatureType = "PLAINTEXT";
        public static readonly string RSASHA1SignatureType = "RSA-SHA1";

        static string CreateSignatureBase(
            ParsedUrl url, 
            string consumerKey, 
            string oAuthToken, 
            string oAuthTokenSecret, 
            HttpMethod httpMethod, 
            string timeStamp, 
            string nonce)
        {
            List<KeyValuePair<string, string>> parameters = ParsedUrl.ParseQueryParams(url.QueryParams);

            parameters.Add(new KeyValuePair<string, string>(OAuthVersionKey, OAuthVersion));
            parameters.Add(new KeyValuePair<string, string>(OAuthNonceKey, nonce));
            parameters.Add(new KeyValuePair<string, string>(OAuthTimestampKey, timeStamp));
            parameters.Add(new KeyValuePair<string, string>(OAuthSignatureMethodKey, HMACSHA1SignatureType));
            parameters.Add(new KeyValuePair<string, string>(OAuthConsumerKeyKey, consumerKey));
            parameters.Add(new KeyValuePair<string, string>(OAuthTokenKey, oAuthToken));

            // Check that this sorts ascending
            parameters.Sort((x, y) => String.CompareOrdinal(x.Key, y.Key));

            ParsedUrl normalizedUrl = url.WithQueryParams(parameters);

            if (normalizedUrl.Port.HasValue && 
                ((normalizedUrl.Scheme == "http" && normalizedUrl.Port == 80) || (normalizedUrl.Scheme == "https" && normalizedUrl.Port == 443)))
            {
                normalizedUrl = normalizedUrl.WithPort();
            }

            return httpMethod.Method + "&" + ParsedUrl.UrlEncode(normalizedUrl.AllNoQueryParams) + "&" + ParsedUrl.UrlEncode(normalizedUrl.QueryParams);
        }

        static string CreateHmacSha1Hash(string signatureBase, string consumerSecret, string oAuthTokenSecret)
        {
            HMACSHA1 hmacsha1 = new HMACSHA1();

            hmacsha1.Key = Encoding.ASCII.GetBytes(ParsedUrl.UrlEncode(consumerSecret) + "&" + ParsedUrl.UrlEncode(oAuthTokenSecret));

            byte[] dataBuffer = System.Text.Encoding.ASCII.GetBytes(signatureBase);
            byte[] hashBytes = hmacsha1.ComputeHash(dataBuffer);

            return Convert.ToBase64String(hashBytes);
        }

        public static string GenerateTimeStamp()
        {
            // Default implementation of UNIX time of the current UTC time
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);

            return Convert.ToInt64(ts.TotalSeconds).ToString();            
        }

        public static string GenerateNonce()
        {
            return new Random().Next(123400, 9999999).ToString();            
        }

        public static string GenerateAuthHeader(
            ParsedUrl url, 
            string consumerKey, 
            string consumerSecret, 
            string oAuthToken, 
            string oAuthTokenSecret, 
            HttpMethod httpMethod, 
            string timeStamp,
            string nonce)
        {
            string signatureBase = CreateSignatureBase(
                url, consumerKey, oAuthToken, oAuthTokenSecret, httpMethod, 
                timeStamp, nonce);
            var signature = CreateHmacSha1Hash(signatureBase, consumerSecret, oAuthTokenSecret);
            var authHeader = "OAuth {0}=\"{1}\",{2}=\"{3}\",{4}=\"{5}\",{6}=\"{7}\",{8}=\"{9}\",{10}=\"{11}\",{12}=\"{13}\"".InvariantFormat(
                OAuthConsumerKeyKey, consumerKey,
                OAuthTokenKey, oAuthToken,
                OAuthSignatureKey, ParsedUrl.UrlEncode(signature),
                OAuthSignatureMethodKey, HMACSHA1SignatureType,
                OAuthTimestampKey, timeStamp,
                OAuthNonceKey, nonce,
                OAuthVersionKey, OAuthVersion);

            return authHeader;
        }

        public static string GenerateAuthHeader(
            ParsedUrl url, 
            string consumerKey, 
            string consumerSecret, 
            string oAuthToken, 
            string oAuthTokenSecret, 
            HttpMethod httpMethod)
        {
            return GenerateAuthHeader(url, consumerKey, consumerSecret, oAuthToken, oAuthTokenSecret, httpMethod, 
                GenerateTimeStamp(), GenerateNonce());
        }
    }
}