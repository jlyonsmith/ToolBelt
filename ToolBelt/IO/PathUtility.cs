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
        #region Fields
        public static char[] InvalidPathChars = new char[]
        {
            '"',
            '<',
            '>',
            '|',
            '\0',
            '\u0001',
            '\u0002',
            '\u0003',
            '\u0004',
            '\u0005',
            '\u0006',
            '\a',
            '\b',
            '\t',
            '\n',
            '\v',
            '\f',
            '\r',
            '\u000e',
            '\u000f',
            '\u0010',
            '\u0011',
            '\u0012',
            '\u0013',
            '\u0014',
            '\u0015',
            '\u0016',
            '\u0017',
            '\u0018',
            '\u0019',
            '\u001a',
            '\u001b',
            '\u001c',
            '\u001d',
            '\u001e',
            '\u001f'
        };
        public static char[] InvalidFileNameChars = new char[]
        {
            '/',
            '\\',
            '"',
            '<',
            '>',
            '|',
            '\0',
            '\u0001',
            '\u0002',
            '\u0003',
            '\u0004',
            '\u0005',
            '\u0006',
            '\a',
            '\b',
            '\t',
            '\n',
            '\v',
            '\f',
            '\r',
            '\u000e',
            '\u000f',
            '\u0010',
            '\u0011',
            '\u0012',
            '\u0013',
            '\u0014',
            '\u0015',
            '\u0016',
            '\u0017',
            '\u0018',
            '\u0019',
            '\u001a',
            '\u001b',
            '\u001c',
            '\u001d',
            '\u001e',
            '\u001f'
        };
        public static readonly char[] BadDirTrailChars = new char[] {'.', ' ', '\t'};
        public static readonly char[] WildcardChars = new char[] {'*', '?'};

        #endregion

        #region Constructors
        // No need to construct this object
        private PathUtility()
        {
        }
        #endregion

        /// <summary>
        /// Prefix for UNC file paths. 
        /// </summary>
        public static readonly string UncPrefix = new string (Path.DirectorySeparatorChar, 2);

        /// <summary>
        /// Character used to separate the directories
        /// </summary>
        public static readonly char DirectorySeparatorChar = Path.DirectorySeparatorChar;
        public static readonly string DirectorySeparator = Path.DirectorySeparatorChar.ToString();

		/// <summary>
		/// Alternate character used to separate the directories
		/// </summary>
		public static readonly char AltDirectorySeparatorChar = '\\';
		public static readonly string AltDirectorySeparator = AltDirectorySeparatorChar.ToString();

        /// <summary>
        /// Character used to separate the paths
        /// </summary>
		public static readonly char PathSeparatorChar = ':';
		public static readonly string PathSeparator = PathSeparatorChar.ToString();

		/// <summary>
		/// Alternate character used as the path separator
		/// </summary>
        public static readonly char AltPathSeparatorChar = ';';
		public static readonly string AltPathSeparator = AltPathSeparatorChar.ToString();
        
        /// <summary>
        /// Character used to separate the file extension from the main part of the file name.
        /// </summary>
        public static readonly char ExtensionSeparatorChar = '.';
        public static readonly string ExtensionSeparator = ExtensionSeparatorChar.ToString();

        /// <summary>
        /// The drive separator character. Only Windows platforms use this.
        /// </summary>
        public static readonly char DriveSeparatorChar = ':';
        public static readonly string DriveSeparator = DriveSeparatorChar.ToString();

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
