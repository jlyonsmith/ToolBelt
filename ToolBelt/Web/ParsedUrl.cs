using System;
using System.Net.Mail;
using System.Net;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ToolBelt;
using System.Text;

namespace ToolBelt
{
	public sealed class ParsedUrl
	{
        public static readonly string UnencodedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

        static Regex reUrl = new Regex(
            @"^(?'scheme'[a-z]+)://((?'user'.+?)(:(?'password'.+?))?@)?((?'host'[a-z\.0-9-\*]+)(:(?'port'\d+))?)?((?'path'/.*?)?(\?(?'params'.+))?)?$", 
            RegexOptions.ExplicitCapture | RegexOptions.Singleline);
        static Regex reParam = new Regex(
            @"(?'key'[A-Za-z0-9_-]+?)(=(?'newPath'.*?))*?(&|$)", 
            RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        string url;
        short userIndex;
        short passwordIndex;
        short hostIndex;
        short portIndex;
        short pathIndex;
        short queryIndex;

        public string Scheme 
        {
            get
            {
                return url.Substring(0, (userIndex > 0 ? userIndex - 3 : hostIndex > 0 ? hostIndex - 3 : pathIndex - 3));
            }
        }

        public string User 
        { 
            get
            {
                if (userIndex < 0)
                    return null;
                else 
                    return url.Substring(userIndex, (passwordIndex > 0 ? passwordIndex - 1 : hostIndex - 1) - userIndex);
            }
        }

        public string Password
        { 
            get
            {
                if (passwordIndex < 0)
                    return null;
                else
                    return url.Substring(passwordIndex, (hostIndex - 1) - passwordIndex);
            }
        }

        public string UserAndPassword
        {
            get
            {
                var user = User;
                var password = Password;

                if (user != null && password == null)
                    return user + "@";
                else if (user != null && password != null)
                    return user + ":" + password + "@";
                else
                    return "";
            }
        }

        public string Host
        { 
            get
            {
                if (hostIndex < 0)
                    return null;

                return url.Substring(hostIndex, (portIndex > 0 ? portIndex - 1 : pathIndex > 0 ? pathIndex : url.Length) - hostIndex);
            }
        }

        public int? Port 
        { 
            get
            {
                if (portIndex < 0) 
                    return null;

                return ushort.Parse(url.Substring(portIndex, (pathIndex > 0 ? pathIndex : url.Length) - portIndex));
            }
        }

        public string Path 
        { 
            get
            {
                if (pathIndex < 0)
                    return null;

                return url.Substring(pathIndex, (queryIndex > 0 ? queryIndex - 1 : url.Length) - pathIndex);
            }
        }

        public string QueryParams 
        { 
            get
            {
                if (queryIndex < 0)
                    return null;

                return url.Substring(queryIndex);
            }
        }

        public string HostAndPort
        {
            get 
            {
                var host = this.Host;
                var port = this.Port;

                if (host == null)
                    return null;
                else if (port == null)
                    return host;
                else
                    return host + ":" + port.Value.ToString();
            }
        }

        public string All 
        {
            get
            {
                var queryParams = QueryParams;

                if (queryParams == null)
                    queryParams = "";
                else
                    queryParams = "?" + queryParams;

                return AllNoQueryParams + queryParams;
            }
        }

        public string AllNoQueryParams 
        {
            get
            {
                var path = Path;

                if (path == null)
                    path = "";

                return Scheme + "://" + UserAndPassword + HostAndPort + path;
            }
        }

        public static string CombineAsQueryParams(List<KeyValuePair<string, string>> pairs)
        {
            StringBuilder sb = new StringBuilder();

            AppendQueryParams(sb, pairs);

            return sb.ToString();
        }

        static void AppendQueryParams(StringBuilder sb, List<KeyValuePair<string, string>> queryParams)
        {
            for (int i = 0; i < queryParams.Count; i++)
            {
                sb.Append(queryParams[i].Key);
                sb.Append("=");

                if (queryParams[i].Value.Length > 0)
                    sb.Append(queryParams[i].Value);

                if (i < queryParams.Count - 1)
                    sb.Append("&");
            }
        }

        public ParsedUrl(string url)
        {
            var match = reUrl.Match(url);

            if (!match.Success)
                throw new ArgumentException("Invalid URL format");

            var scheme = match.Groups["scheme"].Value;
            var host = match.Groups["host"].Value;
            var port = match.Groups["port"].Value;
            var user = match.Groups["user"].Value;
            var password = match.Groups["password"].Value;
            var path = match.Groups["path"].Value;
            var queryParams = ParseQueryParams(match.Groups["params"].Value);

            StringBuilder sb = new StringBuilder();

            sb.Append(scheme.ToLower());
            sb.Append("://");

            if (user.Length > 0)
            {
                userIndex = (short)sb.Length;
                sb.Append(user);

                if (password.Length > 0)
                {
                    sb.Append(":");
                    passwordIndex = (short)sb.Length;
                    sb.Append(password);
                }
                else
                    passwordIndex = -1;

                sb.Append("@");
            }
            else
            {
                userIndex = passwordIndex = -1;
            }

            if (host.Length > 0)
            {
                hostIndex = (short)sb.Length;
                sb.Append(host);
                
                if (port.Length > 0)
                {
                    sb.Append(":");
                    portIndex = (short)sb.Length;
                    sb.Append(port);
                }
                else
                    portIndex = -1;
            }
            else
            {
                hostIndex = -1;
                portIndex = -1;
            }

            if (path.Length > 0)
            {
                pathIndex = (short)sb.Length;

                if (!path.StartsWith("/"))
                    sb.Append("/");

                sb.Append(path);
            }
            else
                pathIndex = -1;

            if (queryParams.Count != 0)
            {
                sb.Append("?");

                queryIndex = (short)sb.Length;

                AppendQueryParams(sb, queryParams);
            }
            else
                queryIndex = -1;

            this.url = sb.ToString();
        }

        private ParsedUrl(string newUrl, short newUserIndex, short newPasswordIndex, 
            short newHostIndex, short newPortIndex, short newPathIndex, short newQueryIndex)
        {
            url = newUrl;
            userIndex = newUserIndex;
            passwordIndex = newPasswordIndex;
            hostIndex = newHostIndex;
            portIndex = newPortIndex;
            pathIndex = newPathIndex;
            queryIndex = newQueryIndex;
        }

        public static List<KeyValuePair<string, string>> ParseQueryParams(string s)
        {
            var pairs = new List<KeyValuePair<string, string>>();

            if (String.IsNullOrEmpty(s))
                return pairs;

            var match = reParam.Match(s);

            while (match.Success)
            {
                var key = match.Groups["key"].Value;
                var newPath = (match.Groups["newPath"].Value ?? "");

                pairs.Add(new KeyValuePair<string, string>(key, newPath));
                match = match.NextMatch();
            }

            return pairs;
        }

        public override string ToString()
        {
            return this.url;
        }

        public static implicit operator String(ParsedUrl url)
        {
            if (url == null)
                return null;

            return url.ToString();
        }

        public ParsedUrl WithPath(string newPath)
        {
            StringBuilder sb = new StringBuilder(url.Length + newPath.Length);

            sb.Append(url.Substring(0, (pathIndex > 0 ? pathIndex : url.Length)));

            short newPathIndex = (short)sb.Length;

            if (!newPath.StartsWith("/"))
                sb.Append("/");

            sb.Append(newPath);

            short newQueryIndex;

            if (queryIndex > 0)
            {
                sb.Append("?");
                newQueryIndex = (short)sb.Length;
                sb.Append(url.Substring(queryIndex));
            }
            else
                newQueryIndex = -1;

            return new ParsedUrl(sb.ToString(), userIndex, passwordIndex, hostIndex, portIndex, newPathIndex, newQueryIndex);
        }

        public ParsedUrl WithPort(int? port = null)
        {
            var s = (port.HasValue ? ":" + port.Value.ToString() : "");
            var sb = new StringBuilder(url.Length + s.Length);

            sb.Append(url.Substring(0, portIndex != -1 ? portIndex - 1 : pathIndex != -1 ? pathIndex : queryIndex != -1 ? queryIndex - 1 : url.Length));

            short newPortIndex = -1, newPathIndex = -1, newQueryIndex = -1;

            if (port.HasValue)
            {
                newPortIndex = (short)(sb.Length + 1);
                sb.Append(s);
            }

            if (pathIndex >= 0)
            {
                newPathIndex = (short)sb.Length;
                sb.Append(this.Path);
            }

            if (queryIndex >= 0)
            {
                newQueryIndex = (short)sb.Length;
                sb.Append(this.QueryParams);
            }

            return new ParsedUrl(sb.ToString(), userIndex, passwordIndex, hostIndex, newPortIndex, newPathIndex, newQueryIndex);
        }

        public ParsedUrl WithQueryParams(string queryParams)
        {
            return WithQueryParams(ParseQueryParams(queryParams));
        }

        public ParsedUrl WithQueryParams(List<KeyValuePair<string, string>> queryParams)
        {
            var sb = new StringBuilder(url.Length);
            short newQueryIndex = -1;

            sb.Append(AllNoQueryParams);

            if (queryParams.Count > 0)
            {
                sb.Append("?");
                newQueryIndex = (short)sb.Length;

                for (int i = 0; i < queryParams.Count; i++)
                {
                    var queryParam = queryParams[i];

                    sb.AppendFormat("{0}={1}{2}", queryParam.Key, queryParam.Value, i < queryParams.Count - 1 ? "&" : "");
                }
            }

            return new ParsedUrl(sb.ToString(), userIndex, passwordIndex, hostIndex, portIndex, pathIndex, newQueryIndex);
        }

        public static string UrlEncode(string s, bool uppercaseHex = true)
        {
            StringBuilder result = new StringBuilder();
            var format = uppercaseHex ? "{0:X2}" : "{0:x2}";

            foreach (char c in s)
            {
                if (UnencodedChars.IndexOf(c) != -1)
                {
                    result.Append(c);
                }
                else
                {
                    result.Append('%' + String.Format(format, (int)c));
                }
            }

            return result.ToString();
        }

        public override bool Equals(object obj)
        {
            ParsedUrl path = obj as ParsedUrl;

            if (path == null)
                return false;

            return this.url.Equals(path.url, StringComparison.InvariantCulture);
        }

        public override int GetHashCode()
        {
            return url.GetHashCode();
        }

        public static bool Equals(ParsedUrl url1, ParsedPath url2)
        {
            if ((object)url1 == (object)url2)
                return true;

            if ((object)url1 == null || (object)url2 == null)
                return false;

            return url1.Equals(url2);
        }
	}
}

