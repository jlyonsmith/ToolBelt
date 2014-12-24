using System;
using System.Text.RegularExpressions;
using System.Text;

namespace ToolBelt
{
    public class ParsedEmail
    {
        static Regex reUrl = new Regex(
            @"^((?'scheme'[a-z]+)://)?(?'user'[a-z0-9\.-]+)@(?'host'[a-z0-9\.-]+)$", 
            RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        string email;
        short userIndex;
        short hostIndex;

        public string Scheme 
        {
            get
            {
                return email.Substring(0, userIndex - 3);
            }
        }

        public string User
        { 
            get
            {
                return email.Substring(userIndex, hostIndex - userIndex - 1);
            }
        }

        public string Host
        { 
            get
            {
                return email.Substring(hostIndex, email.Length - hostIndex);
            }
        }

        public ParsedEmail(string email)
        {
            var match = reUrl.Match(email);

            if (!match.Success)
                throw new ArgumentException("Invalid email format");

            var scheme = match.Groups["scheme"].Value;
            var host = match.Groups["host"].Value;
            var user = match.Groups["user"].Value;

            StringBuilder sb = new StringBuilder();

            if (scheme.Length == 0)
                scheme = "mail";

            sb.Append(scheme);
            sb.Append("://");

            userIndex = (short)sb.Length;
            sb.Append(user);

            sb.Append("@");

            hostIndex = (short)sb.Length;
            sb.Append(host);

            this.email = sb.ToString();
        }

        public string UserAndHost
        {
            get
            {
                return this.User + "@" + this.Host;
            }
        }
        
        public override string ToString()
        {
            return this.email;
        }

        public static implicit operator String(ParsedEmail email)
        {
            if (email == null)
                return null;

            return email.ToString();
        }
    }
}

