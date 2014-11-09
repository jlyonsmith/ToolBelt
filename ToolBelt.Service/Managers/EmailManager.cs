using System;
using System.Net.Mail;
using System.Net;
using System.Collections.Generic;
using ToolBelt;
using System.Text.RegularExpressions;

namespace ToolBelt.Service
{
    public class EmailManager : IEmailManager
    {
        Regex re = new Regex(@"^<h1.*?>(?'title'.*?)</h1>$", RegexOptions.Multiline | RegexOptions.Multiline);

        public ParsedUrl SmtpUrl { get; private set; } 
        public string SupportEmail { get; private set; }

        public EmailManager(IEmailManagerConfig config)
        {
            this.SmtpUrl = config.AwsSesSmtpUrl;
            this.SupportEmail = config.SupportEmail;
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

            // NOTE: If the following code throws then see http://www.mono-project.com/docs/faq/security/ for details on certificates.
            // Avoid doing this, which will silence the error but is NOT a shippable solution:
            // ServicePointManager.ServerCertificateValidationCallback = (obj, cert, chain, error) => true;

            try
            {
                client.Send(message);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}

