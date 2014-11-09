using System;

namespace ToolBelt
{
    public class ParsedEmail
    {
        string email;

        public ParsedEmail()
        {
        }

        public override string ToString()
        {
            return this.email;
        }

        public static implicit operator String(ParsedEmail email)
        {
            if (email == null)
                return null;

            return email.ToString();
        }
    }
}

