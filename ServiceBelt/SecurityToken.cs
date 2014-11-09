using System;
using ServiceStack;
using ServiceStack.Web;
using System.Collections.Generic;
using JWT;
using System.Globalization;
using Rql;

namespace ServiceBelt
{
    public class SecurityToken
    {
        public string UserEmail { get; private set; }
        public RqlId UserId { get; private set; }
        public DateTime? ExpiresAtUtc { get; private set; }

        public SecurityToken(string userEmail, RqlId userId, TimeSpan? expiresIn = null)
        {
            UserEmail = userEmail;
            UserId = userId;

            if (expiresIn.HasValue)
                ExpiresAtUtc = DateTime.UtcNow.Add(expiresIn.Value);
            else
                ExpiresAtUtc = null;
        }
    }
}

