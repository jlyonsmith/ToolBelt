using System;
using ServiceStack.Web;
using Rql;

namespace ToolBelt.Service
{
    public interface ISecuredUser
	{
        RqlId Id { get; set; } 
        string Email { get; set; }
        int Role { get; set; }
	}
}

