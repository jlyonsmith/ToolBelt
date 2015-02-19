using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

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

        public static void ForEach(this IEnumerable enumeration, Action<object> action)
        {
            foreach (var item in enumeration)
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

