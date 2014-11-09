using System;
using System.Collections.Generic;
using System.Net;
using ServiceStack;
using ToolBelt;
using ServiceStackService = global::ServiceStack.Service;
using ServiceStack.FluentValidation;
using ServiceStack.Auth;

namespace ServiceBelt
{
	public interface IEmailManager
	{
        bool Send(string to, string template, Dictionary<string, string> variables);
	}

}

