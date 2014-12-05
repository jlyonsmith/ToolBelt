using System;

namespace ServiceBelt
{
    public interface ITokenManager
    {
        SecurityToken ToSecurityToken(string jwtToken, string keyName);
        string ToJwtToken(SecurityToken token, string keyName);
    }
}

