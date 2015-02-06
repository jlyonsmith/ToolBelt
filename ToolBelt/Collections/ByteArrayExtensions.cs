using System;

namespace ToolBelt
{
    public static class ByteArrayExtensions
    {
        public static string ToHex(this byte[] p)
        {
            char[] c = new char[p.Length * 2];
            byte b;

            for (int y = 0, x = 0; y < p.Length; ++y, ++x)
            {
                b = ((byte)(p[y] >> 4));
                c[x] = (char)(b > 9 ? b + 'a' - 10 : b + '0');
                b = ((byte)(p[y] & 0xF));
                c[++x] = (char)(b > 9 ? b + 'a' - 10 : b + '0');
            }

            return new string(c);
        }
    }
}

