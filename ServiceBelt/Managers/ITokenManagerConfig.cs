using System;
using System.Collections.Generic;
using JWT;
using Rql;

namespace ServiceBelt
{
	public interface ITokenManagerConfig
	{
        string LoginTokenSecret { get; set; }
        string VerifyEmailTokenSecret { get; set; }
        string ResetPasswordTokenSecret { get; set; }
	}
 }

