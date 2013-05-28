using System;
using System.Collections.Generic;

namespace Playroom
{
    public static class EnumerationHelper
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

