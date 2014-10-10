using System;

namespace ToolBelt
{
    public static class Base62KeyGenerator
    {
        static readonly string digits = "0987654321ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        public static string Generate(int length)
        {
            var random = new Random();
            var c = new char[length];

            for (int i = 0; i < length; i++)
                c[i] = digits[random.Next(digits.Length)];

            return new string(c);
        }
    }
}

