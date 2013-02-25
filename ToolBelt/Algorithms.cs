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
                throw new NotImplementedException();
            }
            public void Reset()
            {
                throw new NotImplementedException();
            }
            public object Current
            {
                get 
                {
                    throw new NotImplementedException();
                }
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
                get
                {
                    throw new NotImplementedException();
                }
            }

            #endregion
        }

        public static IEnumerable Permute(IList list)
        {
            throw new NotImplementedException();
        }
    }
}

