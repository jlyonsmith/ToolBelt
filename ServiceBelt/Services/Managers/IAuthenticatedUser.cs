using System;
using ServiceStack.Web;
using Rql;

namespace ServiceBelt
{
    public interface IAuthenticatedUser
	{
        RqlId Id { get; } 
        string Email { get; }
        int Role { get; }
	}
}

