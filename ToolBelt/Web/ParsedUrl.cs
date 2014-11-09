using System;
using System.Net.Mail;
using System.Net;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ToolBelt;
using System.Text;

namespace ToolBelt
{
	public class ParsedUrl
	{
        static Regex reUrl = new Regex(
            @"^(?'scheme'[a-z]+)://((?'user'.+?)(:(?'password'.+?))?@)?((?'host'[a-z\.0-9-\*]+)(:(?'port'\d+))?)?((?'path'/.*?)?(\?(?'params'.+))?)?$", 
            RegexOptions.ExplicitCapture);
        static Regex reParam = new Regex(
            @"(?'key'[A-Za-z0-9_-]+?)(=(?'newPath'.*?))*?(&|$)", 
            RegexOptions.ExplicitCapture);

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

        public string Query 
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

            sb.Append(scheme);
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

            if (queryParams != null)
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

        List<KeyValuePair<string, string>> ParseQueryParams(string s)
        {
            if (String.IsNullOrEmpty(s))
                return null;

            var match = reParam.Match(s);
            var pairs = new List<KeyValuePair<string, string>>();

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

        public ParsedUrl WithQuery(string newQuery)
        {
            StringBuilder sb = new StringBuilder(url.Length + newQuery.Length);

            sb.Append(url.Substring(0, (queryIndex > 0 ? queryIndex - 1 : url.Length)));
            sb.Append("?");

            short newQueryIndex = (short)sb.Length;
            var queryParams = ParseQueryParams(newQuery);

            AppendQueryParams(sb, queryParams);

            return new ParsedUrl(sb.ToString(), userIndex, passwordIndex, hostIndex, portIndex, pathIndex, newQueryIndex);
        }
	}
}

