using System;

namespace ToolBelt.Service
{
    public interface ITokenManager
    {
        SecurityToken ToLoginToken(string jwtToken);
        SecurityToken ToVerifyEmailToken(string jwtToken);
        SecurityToken ToResetPasswordToken(string jwtToken);
        string ToLoginJwt(SecurityToken token);
        string ToVerifyEmailJwt(SecurityToken token);
        string ToResetPasswordJwt(SecurityToken token);
    }
}

