namespace ToolBelt
{
	using System;
	using System.Runtime.InteropServices;

	/// <summary>
	/// Summary description for Memory.
	/// </summary>
	public unsafe sealed class Memory
	{
		#region NativeMethods
		class NativeMethods
		{
			[DllImport("kernel32.dll")]
			public unsafe static extern void CopyMemory(byte* pDst, byte* pSrc, int len);
		}
		
		#endregion
		
		private Memory()
		{
		}

		/// <summary>
		/// Calculate length of a zero terminated string, C-style string.
		/// </summary>
		/// <param name="p"></param>
		/// <returns>Length of the string.</returns>
		private static int StringLength(char* p)
		{
			int len = 0;
			
			while (*p != 0) 
				len++;
			
			return len;
		}

		/// <summary>
		/// Copy a string object to a char* location.   String is always zero terminated, even if source is 
		/// bigger than <code>len</code> characters.  Copying stops at first zero found in source characters.
		/// </summary>
		/// <param name="pDst">Destination</param>
		/// <param name="src">Source</param>
		/// <param name="len">Maximum number of characters (including terminating zero) to copy.</param>
		public static void Copy(char* pDst, string src, int len)
		{
			if (len == 0)
				return;
		
			// We might as well use a pointer, as this is an unsafe method anyway!
			fixed (char* pTmp = src)
			{
				// pTmp is read-only!
				char* pSrc = pTmp;
				
				while ((len != 0) && (*pSrc != 0))
				{
					*pDst++= *pSrc++;
					len--;
				}

				if (len == 0)
					pDst--;

				*pDst = '\0';
			}
		}

		/// <summary>
		/// Copy a byte array to a byte* location.
		/// </summary>
		/// <param name="pDst">Destination</param>
		/// <param name="src">Source</param>
		/// <param name="len">Maximum number of bytes to copy</param>
		public static void Copy(byte* pDst, byte[] src, int len)
		{
			fixed (byte* pTmp = src)
			{
				NativeMethods.CopyMemory(pDst, pTmp, len);
			}
		}

		/// <summary>
		/// Copy a shart array to a short* location.
		/// </summary>
		/// <param name="pDst">Destination</param>
		/// <param name="src">Source</param>
		/// <param name="len">Maximum number of bytes to copy</param>
		public static void Copy(short* pDst, short[] src, int len)
		{
			fixed (short* pTmp = src)
			{
				NativeMethods.CopyMemory((byte*)pDst, (byte*)pTmp, len * sizeof(short));
			}
		}

		/// <summary>
		/// Copy a int array to an int* location.
		/// </summary>
		/// <param name="pDst">Destination</param>
		/// <param name="src">Source</param>
		/// <param name="len">Maximum number of bytes to copy</param>
		public static void Copy(int* pDst, int[] src, int len)
		{
			fixed (int* pTmp = src)
			{
				NativeMethods.CopyMemory((byte*)pDst, (byte*)pTmp, len * sizeof(int));
			}
		}

		/// <summary>
		/// Copy a long array to a long* location.
		/// </summary>
		/// <param name="pDst">Destination</param>
		/// <param name="src">Source</param>
		/// <param name="len">Maximum number of bytes to copy</param>
		public static void Copy(long* pDst, long[] src, int len)
		{
			fixed (long* pTmp = src)
			{
				NativeMethods.CopyMemory((byte*)pDst, (byte*)pTmp, len * sizeof(long));
			}
		}

		/// <summary>
		/// Copy a float array to a float* location.
		/// </summary>
		/// <param name="pDst">Destination</param>
		/// <param name="src">Source</param>
		/// <param name="len">Maximum number of bytes to copy</param>
		public static void Copy(float* pDst, float[] src, int len)
		{
			fixed (float* pTmp = src)
			{
				NativeMethods.CopyMemory((byte*)pDst, (byte*)pTmp, len * sizeof(float));
			}
		}

		/// <summary>
		/// Copy a double array to a double* location.
		/// </summary>
		/// <param name="pDst">Destination</param>
		/// <param name="src">Source</param>
		/// <param name="len">Maximum number of bytes to copy</param>
		public static void Copy(double* pDst, double[] src, int len)
		{
			fixed (double* pTmp = src)
			{
				NativeMethods.CopyMemory((byte*)pDst, (byte*)pTmp, len * sizeof(double));
			}
		}

		/// <summary>
		/// Compare one string with another.  Assumes both strings have a terminating zero character.
		/// </summary>
		/// <returns>0 if strings are equal, -1 if first string &lt; second string, 1 otherwise.</returns>
		/// <param name="pStr1">First string</param>
		/// <param name="pStr2">Second string</param>
		public static int Compare(char* pStr1, char* pStr2)
		{
			int n = 0;
			
			while ((n == 0) && (*pStr1 != '\0') && (*pStr2 != '\0'))
			{
				n = (*pStr1++ - *pStr2++);
			}
			
			// If they're different, return
			if (n != 0)
				return (n > 0 ? 1 : -1);
			
			// They look the same so far, but one could be longer	
			if (*pStr1 != '\0')
				return 1;  // "abcdefg" > "abc"
			else if (*pStr2 != '\0')
				return -1; // "abc" < "abcdefg"
			else
				return 0; // They're the same
		}

		/// <summary>
		/// Compare two blocks of memory.
		/// </summary>
		/// <returns><code>length</code> if memory blocks are equal, otherwise the offset of the first unequal byte.</returns>
		/// <param name="pMem1">Pointer to first block of memory</param>
		/// <param name="pMem2">Pointer to second block of memory</param>
		/// <param name="length">Number of bytes to compare</param>
		public static int Compare(byte* pMem1, byte* pMem2, int length)
		{
			int i; 
			
			for (i = 0; i < length; i++)
			{
				if (*pMem1++ != *pMem2++)
					break;
			}
			
			return i;
		}
		
		/// <summary>
		/// Compare two blocks of memory containing short values.
		/// </summary>
		/// <returns><code>length</code> if memory blocks are equal, otherwise the offset of the first unequal short.</returns>
		/// <param name="pMem1">Pointer to first block of memory</param>
		/// <param name="pMem2">Pointer to second block of memory</param>
		/// <param name="length">Number of bytes to compare</param>
		public static int Compare(short* pMem1, short* pMem2, int length)
		{
			return Compare((byte*)pMem1, (byte*)pMem2, length * sizeof(short)) / sizeof(short);
		}
		
		/// <summary>
		/// Compare two blocks of memory containing int values
		/// </summary>
		/// <returns><code>length</code> if memory blocks are equal, otherwise the offset of the first unequal int.</returns>
		/// <param name="pMem1">Pointer to first block of memory</param>
		/// <param name="pMem2">Pointer to second block of memory</param>
		/// <param name="length">Number of bytes to compare</param>
		public static int Compare(int* pMem1, int* pMem2, int length)
		{
			return Compare((byte*)pMem1, (byte*)pMem2, length * sizeof(int)) / sizeof(int);
		}
		
		/// <summary>
		/// Compare two blocks of memory containing long values
		/// </summary>
		/// <returns><code>length</code> if memory blocks are equal, otherwise the offset of the first unequal long.</returns>
		/// <param name="pMem1">Pointer to first block of memory</param>
		/// <param name="pMem2">Pointer to second block of memory</param>
		/// <param name="length">Number of bytes to compare</param>
		public static int Compare(long* pMem1, long* pMem2, int length)
		{
			return Compare((byte*)pMem1, (byte*)pMem2, length * sizeof(long)) / sizeof(long);
		}
		
		/// <summary>
		/// Compare two blocks of memory containing float values.
		/// </summary>
		/// <returns><code>length</code> if memory blocks are equal, otherwise the offset of the first unequal float.</returns>
		/// <param name="pMem1">Pointer to first block of memory</param>
		/// <param name="pMem2">Pointer to second block of memory</param>
		/// <param name="length">Number of bytes to compare</param>
		public static int Compare(float* pMem1, float* pMem2, int length)
		{
			return Compare((byte*)pMem1, (byte*)pMem2, length * sizeof(float)) / sizeof(float);
		}
		
		/// <summary>
		/// Compare two blocks of memory containing doubles.
		/// </summary>
		/// <returns><code>length</code> if memory blocks are equal, otherwise the offset of the first unequal double.</returns>
		/// <param name="pMem1">Pointer to first block of memory</param>
		/// <param name="pMem2">Pointer to second block of memory</param>
		/// <param name="length">Number of bytes to compare</param>
		public static int Compare(double* pMem1, double* pMem2, int length)
		{
			return Compare((byte*)pMem1, (byte*)pMem2, length * sizeof(double)) / sizeof(double);
		}
	}
}
