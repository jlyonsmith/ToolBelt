using System;
using System.Collections.Generic;
using JWT;
using Rql;
using ToolBelt;

namespace ServiceBelt
{
    public class TokenManager : ITokenManager
    {
        private DateTime unixEpochUtc = new DateTime(1970, 1, 1, 0, 0 ,0, 0, DateTimeKind.Utc);
        private Dictionary<string, string> secretKeys = new Dictionary<string, string>();

        public TokenManager(IEnumerable<KeyValuePair<string, string>> secretKeyPairs)
        {
            foreach (var secretKeyPair in secretKeyPairs)
            {
                secretKeys[secretKeyPair.Key] = (String.IsNullOrEmpty(secretKeyPair.Value) || secretKeyPair.Value == "*" ? 
                    Base62KeyGenerator.Generate(40) : secretKeyPair.Value);
            }
        }

        Dictionary<string, object> TokenToPayload(SecurityToken token)
        {
            var payload = new Dictionary<string, object>() {
                { "prn", token.UserEmail },
                { "jti", token.UserId.ToString() }
            };

            if (token.ExpiresAtUtc.HasValue)
                payload.Add("exp", (token.ExpiresAtUtc.Value - unixEpochUtc).TotalSeconds);

            return payload;
        }

        SecurityToken PayloadToToken(Dictionary<string, object> dict)
        {
            TimeSpan? exp = (dict.ContainsKey("exp") ? (TimeSpan?)TimeSpan.FromSeconds(decimal.ToDouble((decimal)dict["exp"])) : null);

            var email = (string)dict["prn"];
            var id = RqlId.Parse((string)dict["jti"]);

            return new SecurityToken(email, id, exp);
        }

        public SecurityToken ToSecurityToken(string jwtToken, string keyName)
        {
            var dict = JsonWebToken.DecodeToObject(jwtToken, secretKeys[keyName], verify: true) as Dictionary<string, object>;

            return PayloadToToken(dict);
        }

        public string ToJwtToken(SecurityToken token, string keyName)
        {
            return JsonWebToken.Encode(TokenToPayload(token), secretKeys[keyName], JWT.JwtHashAlgorithm.HS256);
        }
    }
}

