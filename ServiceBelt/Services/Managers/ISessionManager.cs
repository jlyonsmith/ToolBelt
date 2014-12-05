using System;
using ServiceStack.Web;
using Rql;

namespace ServiceBelt
{
    public interface ISessionManager
    {
        IAuthenticatedUser GetLoggedInUser(IRequest request);
        T GetLoggedInUserAs<T>(IRequest request) where T : class;
        void UpdateLoggedInUser(IAuthenticatedUser user);
        string LoginUser(IAuthenticatedUser user);
        void LogoutUser(IRequest request); 
        void LogoutUser(RqlId id); 
    }
}

