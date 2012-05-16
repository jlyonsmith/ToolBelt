using System;
using System.IO;
using System.Collections;

namespace ToolBelt
{
	/// <summary>
	/// Search scope in which to look for files.
	/// </summary>
	public enum SearchScope
	{
		/// <summary>
		/// Search only the directory specified in the file specification
		/// </summary>
		DirectoryOnly,
		/// <summary>
		/// Search the directory in the file specification and all children recursively using a breadth
        /// first algorithm.
		/// </summary>
		RecurseSubDirectoriesBreadthFirst,
		/// <summary>
		/// Search the directory in the file specification and all direct parent directories
		/// </summary>
		RecurseParentDirectories,
		/// <summary>
		/// Search the directory in the file specification and all children recursively using a depth
        /// first algorithm.
		/// </summary>
		RecurseSubDirectoriesDepthFirst,
	}

	/// <summary>
	/// Extensions to the <see cref="Directory"/> class.
	/// </summary>
	public sealed class DirectoryUtility
	{
		/// <summary>
		/// See <see cref="DirectoryInfoUtility.GetFiles(string, SearchScope)"/>.
		/// </summary>
		/// <param name="fileSpec"></param>
		/// <param name="scope"></param>
		/// <returns></returns>
		public static string[] GetFiles(string path, string searchPattern, SearchScope scope)
		{
			FileInfo[] fileInfos = DirectoryInfoUtility.GetFiles(path, searchPattern, scope);

            string[] files = new string[fileInfos.Length];

            for (int i = 0; i < fileInfos.Length; i++)
                files[i] = fileInfos[i].FullName;

            return files;
		}
		
		/// <summary>
		/// See <see cref="DirectoryInfoUtility.GetDirectories(string, SearchScope)"/>
		/// </summary>
		/// <param name="dirSpec"></param>
		/// <param name="scope"></param>
		/// <returns></returns>
		public static string[] GetDirectories(string path, string searchPattern, SearchScope scope)
		{
			DirectoryInfo[] dirInfos = DirectoryInfoUtility.GetDirectories(path, searchPattern, scope);

            string[] dirs = new string[dirInfos.Length];

            for (int i = 0; i < dirInfos.Length; i++)
                dirs[i] = dirInfos[i].FullName;

            return dirs;
        }
	}
}
