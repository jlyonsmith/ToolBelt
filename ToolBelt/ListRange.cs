using System;
using System.Collections.Generic;
using System.Collections;

namespace ToolBelt
{
    // TODO: An implicit conversion to T[] would be good to add 
    public class ListRange<T> : IEnumerable<T>
    {
        private IList<T> original;
        private int start;

        public ListRange(IList<T> list, int start)
        {
            if (start >= list.Count)
                throw new ArgumentOutOfRangeException("start");

            this.original = list;
            this.start = start;
            Count = original.Count - start;
        }

        public ListRange(IList<T> list, int start, int count)
        {
            if (start >= list.Count)
                throw new ArgumentOutOfRangeException("start");

            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            this.original = list;
            this.start = start;
            Count = count;
        }

        public T this[int index] 
        {
            get 
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();
                return original[start + index];
            }
        }
        
        public int Count { get; private set; }
        
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++) 
            {
                yield return original[start + i];
            }
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

