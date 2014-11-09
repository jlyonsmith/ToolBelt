using System;
using System.Collections.Generic;
using JWT;
using Rql;
using ToolBelt;

namespace ToolBelt.Service
{
    public class TokenManager : ITokenManager
    {
        private DateTime unixEpochUtc = new DateTime(1970, 1, 1, 0, 0 ,0, 0, DateTimeKind.Utc);
        private string loginSecretKey;
        private string verifyEmailSecretKey;
        private string resetPasswordSecurityKey;

        public TokenManager(ITokenManagerConfig config)
        {
            loginSecretKey = (config.LoginTokenSecret == "*" ? Base62KeyGenerator.Generate(40) : config.LoginTokenSecret);
            verifyEmailSecretKey = (config.VerifyEmailTokenSecret == "*" ? Base62KeyGenerator.Generate(40) : config.VerifyEmailTokenSecret);
            resetPasswordSecurityKey = (config.ResetPasswordTokenSecret == "*" ? Base62KeyGenerator.Generate(40) : config.ResetPasswordTokenSecret);
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

        public SecurityToken ToLoginToken(string jwtToken)
        {
            var dict = JsonWebToken.DecodeToObject(jwtToken, loginSecretKey, verify: true) as Dictionary<string, object>;

            return PayloadToToken(dict);
        }

        public SecurityToken ToVerifyEmailToken(string jwtToken)
        {
            var dict = JsonWebToken.DecodeToObject(jwtToken, verifyEmailSecretKey, verify: true) as Dictionary<string, object>;

            return PayloadToToken(dict);
        }

        public SecurityToken ToResetPasswordToken(string jwtToken)
        {
            var dict = JsonWebToken.DecodeToObject(jwtToken, resetPasswordSecurityKey, verify: true) as Dictionary<string, object>;

            return PayloadToToken(dict);
        }

        public string ToLoginJwt(SecurityToken token)
        {
            return JsonWebToken.Encode(TokenToPayload(token), loginSecretKey, JWT.JwtHashAlgorithm.HS256);
        }

        public string ToVerifyEmailJwt(SecurityToken token)
        {
            return JsonWebToken.Encode(TokenToPayload(token), verifyEmailSecretKey, JWT.JwtHashAlgorithm.HS256);
        }

        public string ToResetPasswordJwt(SecurityToken token)
        {
            return JsonWebToken.Encode(TokenToPayload(token), resetPasswordSecurityKey, JWT.JwtHashAlgorithm.HS256);
        }    
    }
}

