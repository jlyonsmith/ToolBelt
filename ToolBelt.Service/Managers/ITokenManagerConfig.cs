using System;
using System.Collections.Generic;
using JWT;
using Rql;

namespace ToolBelt.Service
{
	public interface ITokenManagerConfig
	{
        string LoginTokenSecret { get; set; }
        string VerifyEmailTokenSecret { get; set; }
        string ResetPasswordTokenSecret { get; set; }
	}
 }

