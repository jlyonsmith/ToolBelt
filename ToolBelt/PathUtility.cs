using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

namespace ToolBelt
{
	/// <summary>
	/// Extends the functionality of <see cref="System.IO.Path"/>. 
	/// </summary>
	public sealed class PathUtility
	{
		#region Constructors
		// No need to construct this object
		private PathUtility()
		{
		}
		#endregion
		
		/// <summary>
		/// Prefix for UNC file paths. 
		/// </summary>
		public static readonly string UncPrefixChars = new string (Path.DirectorySeparatorChar, 2);
		
		/// <summary>
		/// Character used to separate the directories
		/// </summary>
		public static readonly char DirectorySeparatorChar = Path.DirectorySeparatorChar;

		/// <summary>
		/// Alternate character used to separate the directories ('\' on Unix, '/' on Windows)
		/// </summary>
#if WINDOWS
		public static readonly char AltDirectorySeparatorChar = '//';
#else
		public static readonly char AltDirectorySeparatorChar = '\\';
#endif

		/// <summary>
		/// Character used to separate the file extension from the main part of the file name.
		/// </summary>
		public static readonly char ExtensionSeparatorChar = '.';

		/// <summary>
		/// The volume separator character. Only Windows platforms use this.
		/// </summary>
		public static readonly char VolumeSeparatorChar = ':';

        /// <summary>
        /// Searches multiple directories for a file.
        /// </summary>
        /// <param name="paths">An array of paths to search.  Each path is assumed to be a directory.</param>
        /// <param name="file">The file to search for. Any root or directory portion is ignored.  Wildcards are not allowed.</param>
        /// <returns>An array of just the paths that contain the file, in the same order as the passed in array.</returns>
        public static ParsedPathList FindFileInPaths(ParsedPathList paths, ParsedPath file)
        {
			ParsedPathList foundPaths = new ParsedPathList();

            foreach (ParsedPath path in paths)
			{
				try 
				{
                    ParsedPath fullPath = new ParsedPath(path, PathParts.VolumeAndDirectory).Append(file);
				
					if (File.Exists(fullPath))
                        foundPaths.Add(fullPath);
				}
				catch (ArgumentException)
				{
					// If there is a bad path in the list, ignore it
				}
			}
			
            return foundPaths;
		}
	}
}
