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


        public ListRange(IList<T> original, int start, int count)
        {
            this.original = original;
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

