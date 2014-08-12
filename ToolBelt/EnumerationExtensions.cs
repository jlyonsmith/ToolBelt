using System;
using System.Collections.Generic;

namespace ToolBelt
{
    public static class EnumerationExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
            {
                action(item);
            }
        }
    }
}

