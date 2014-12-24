using System;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Collections.Generic;
using ToolBelt;
using System.Text.RegularExpressions;
using ServiceStack.Logging;

namespace ServiceBelt
{
    public class EmailManager : IEmailManager
    {
        ILog log = LogManager.GetLogger(typeof(EmailManager));

        Regex re = new Regex(@"^<h1.*?>(?'title'.*?)</h1>$", RegexOptions.Multiline | RegexOptions.Multiline);

        public ParsedUrl SmtpUrl { get; private set; } 
        public string SupportEmail { get; private set; }

        public EmailManager(IEmailManagerConfig config)
        {
            this.SmtpUrl = config.AwsSesSmtpUrl;
            this.SupportEmail = config.SupportEmail.UserAndHost;
        }

        public bool Send(string to, string template, Dictionary<string, string> variables = null)
        {
            // Send an email to confirm the email address
            var client = new SmtpClient(SmtpUrl.Host, SmtpUrl.Port.Value)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(SmtpUrl.User, SmtpUrl.Password)
            };

            var match = re.Match(template);

            if (!match.Success)
                throw new ArgumentException("Template must start with an <h1> header for the email title");

            var body = StringUtility.ReplaceTags(template, "{{", "}}", variables, TaggedStringOptions.LeaveUnknownTags);

            MailMessage message = new MailMessage(SupportEmail, to, match.Groups["title"].Value, body);

            message.IsBodyHtml = true;

            // NOTE: To avoid SSL errors, run:
            //
            // certmgr --ssl https://www.amazon.com

            try
            {
                client.Send(message);
            }
            catch (Exception e)
            {
                var sb = new StringBuilder();

                while (e != null)
                {
                    sb.Append("|");
                    sb.AppendLine("  " + e.Message);
                    e = e.InnerException;
                }

                log.Error("Unable to send email to '{0}'{1}".InvariantFormat(to, sb.ToString()));
                return false;
            }

            return true;
        }
    }
}

