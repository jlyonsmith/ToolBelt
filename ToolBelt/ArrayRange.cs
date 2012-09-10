using System;
using System.Collections.Generic;
using System.Collections;

namespace ToolBelt
{
	// TODO: An implicit conversion to T[] would be good to add 
	public class ArrayRange<T> : IEnumerable<T>
	{
		private T[] original;
		private int start;
		
		public ArrayRange(T[] original, int start, int len)
		{
			this.original = original;
			this.start = start;
			Length = len;
		}
		
		public T this[int index] 
		{
			get 
			{
				if (index < 0 || index >= Length)
					throw new IndexOutOfRangeException();
				return original[start + index];
			}
		}
		
		public int Length { get; private set; }
		
		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < Length; i++) 
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

