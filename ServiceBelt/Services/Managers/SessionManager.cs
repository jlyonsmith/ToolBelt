using System;
using ServiceStack.Caching;
using ServiceStack.Web;
using Rql;
using ServiceStack;
using System.Reflection;

namespace ServiceBelt
{
    internal class AuthenticatedUser : IAuthenticatedUser
    {
        public RqlId Id { get; set; }
        public string Email { get; set; }
        public int Role { get; set; }
    }

    public class SessionManager : ISessionManager
    {
        public ICacheClient Cache { get; set; }
        public ITokenManager Token { get; set; }

        readonly string appName;

        public SessionManager()
        {
            appName = Assembly.GetEntryAssembly().GetName().Name;
        }

        private string GetCacheName(RqlId id)
        {
            return "{0}.Login.{1}".Fmt(id.ToString(), appName);
        }

        public string LoginUser(IAuthenticatedUser user)
        {
            Cache.Add(GetCacheName(user.Id), user);

            return Token.ToJwtToken(new SecurityToken(user.Email, user.Id, TimeSpan.FromDays(1)), "login");
        }

        public T GetLoggedInUserAs<T>(IRequest request) where T : class
        {
            object obj;

            if (request.Items.TryGetValue("LoginToken", out obj))
            {
                var token = (SecurityToken)obj;

                return Cache.Get<T>(GetCacheName(token.UserId));
            }
            else
                return null;
        }

        public void UpdateLoggedInUser(IAuthenticatedUser user)
        {
            Cache.Set<IAuthenticatedUser>(GetCacheName(user.Id), user);
        }

        public void LogoutUser(IRequest request)
        {
            var user = GetLoggedInUserAs<AuthenticatedUser>(request);

            if (user != null)
                Cache.Remove(GetCacheName(user.Id));
        }

        public void LogoutUser(RqlId id)
        {
            Cache.Remove(GetCacheName(id));
        }
    }
}

