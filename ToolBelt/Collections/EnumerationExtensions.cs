using System;
using System.Collections.Generic;
using System.Linq;

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

        public static void DisposeItems<T>(IEnumerable<T> list)
        {
            foreach (var item in list)
            {
                IDisposable disposable = item as IDisposable;

                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}

