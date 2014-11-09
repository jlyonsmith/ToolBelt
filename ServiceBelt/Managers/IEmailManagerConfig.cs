using System;
using System.Net.Mail;
using System.Net;
using System.Collections.Generic;
using ToolBelt;
using System.Text.RegularExpressions;

namespace ServiceBelt
{
	public interface IEmailManagerConfig
	{
        ParsedUrl AwsSesSmtpUrl { get; set; }
        ParsedEmail SupportEmail { get; set; }
	}
}

