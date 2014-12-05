using System;
using ServiceStack;
using ServiceStack.Web;
using ServiceStack.Caching;

namespace ServiceBelt
{
    public class LoginTokenFeature : IPlugin
    {
        static readonly string bearerPrefix = "Bearer ";
        static bool alreadyConfigured;

        public ITokenManager tokenManager;

        public LoginTokenFeature(ITokenManager tokenManager)
        {
            this.tokenManager = tokenManager;
        }

        public void Register(IAppHost appHost)
        {
            if (alreadyConfigured) return;
                alreadyConfigured = true;

            appHost.GlobalRequestFilters.Add(ExtractTokenFromRequestFilter);
        }

        public void ExtractTokenFromRequestFilter(IRequest req, IResponse res, object requestDto)
        {
            var auth = req.GetHeader("Authorization");

            if (!String.IsNullOrEmpty(auth) && auth.StartsWith(bearerPrefix) && auth.Length > bearerPrefix.Length)
            {
                SecurityToken loginToken = null;

                try
                {
                    loginToken = tokenManager.ToSecurityToken(auth.Substring(bearerPrefix.Length), "login");
                }
                catch (Exception)
                {
                    throw HttpError.Unauthorized("Login token is invalid");
                }

                // Validate the token is not expired
                if (DateTime.UtcNow > loginToken.ExpiresAtUtc)
                    throw HttpError.Unauthorized("Login token has expired");

                req.SetItem("LoginToken", loginToken);
            }
        }
    }
}

