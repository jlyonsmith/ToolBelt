using System;
using ServiceStack.Caching;
using ServiceStack.Web;
using Rql;
using ServiceStack;
using System.Reflection;

namespace ToolBelt.Service
{
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

        public string LoginUser(ISecuredUser user)
        {
            Cache.Add(GetCacheName(user.Id), user);

            return Token.ToLoginJwt(new SecurityToken(user.Email, user.Id, TimeSpan.FromDays(1)));
        }

        public ISecuredUser GetLoggedInUser(IRequest request)
        {
            object obj;

            if (request.Items.TryGetValue("LoginToken", out obj))
            {
                var token = (SecurityToken)obj;

                return Cache.Get<ISecuredUser>(GetCacheName(token.UserId));
            }
            else
                return null;
        }

        public void UpdateLoggedInUser(ISecuredUser user)
        {
            Cache.Set<ISecuredUser>(GetCacheName(user.Id), user);
        }

        public void LogoutUser(IRequest request)
        {
            var user = GetLoggedInUser(request);

            if (user != null)
                Cache.Remove(GetCacheName(user.Id));
        }

        public void LogoutUser(RqlId id)
        {
            Cache.Remove(GetCacheName(id));
        }
    }
}

