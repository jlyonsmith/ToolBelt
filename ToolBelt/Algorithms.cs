using System;
using System.Collections;
using System.Collections.Generic;

namespace ToolBelt
{
    public static class Algorithms
    {
        private class ClassicPermuter : IEnumerator, IEnumerator<IList>
        {
            #region IEnumerator implementation
            public bool MoveNext()
            {
                if (i == len(a))
                    yield return a;
                else
                {
                    perm_aux(a, i+1, the_list);

                    for j in range(i+1, len(a))
                    {
                        a[i], a[j] = a[j], a[i];
                        perm_aux(a, i+1, the_list);
                        a[i], a[j] = a[j], a[i];
                    }
                }
            }
            public void Reset()
            {
                the_list = []
                perm_aux(a, 0, the_list)
                return the_list
            }
            public object Current
            {
                get;
            }
            #endregion

            #region IDisposable

            public void Dispose()
            {
            }

            #endregion

            #region IEnumerator

            IList IEnumerator<IList>.Current
            {
                get;
            }

            #endregion
        }

        public static IEnumerable Permute(IList list)
        {
        }
    }
}

