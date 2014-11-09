using System;
using Rql;
using ServiceStack;
using ServiceStack.Caching;
using ServiceStack.Web;
using System.Net;
using System.Linq;

namespace ServiceBelt
{
    [AttributeUsage(AttributeTargets.Class)]
    public class LoggedInAttribute : RequestFilterAttribute
	{
        public ISessionManager Session { get; set; }
        public int Role { get; set; }

        public LoggedInAttribute(int role) : base(ApplyTo.All)
        {
            this.Role = role;
        }

        public LoggedInAttribute(ApplyTo applyTo, int role) : base(applyTo)
        {
            this.Role = role;
        }

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            var user = Session.GetLoggedInUser(req);

            if (user == null)
            {
                throw HttpError.Unauthorized("Must be logged in");
            }

            if (user.Role < Role)
            {
                throw HttpError.Unauthorized("Insufficient permissions");
            }
        }
	}
}
