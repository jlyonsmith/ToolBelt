using System;
using ServiceStack.Web;
using Rql;

namespace ToolBelt.Service
{
    public interface ISessionManager
    {
        ISecuredUser GetLoggedInUser(IRequest request);
        void UpdateLoggedInUser(ISecuredUser user);
        string LoginUser(ISecuredUser user);
        void LogoutUser(IRequest request); 
        void LogoutUser(RqlId id); 
    }
}

