using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

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
		public static IList<ParsedPath> GetFiles(ParsedPath fileSpec, SearchScope scope)
		{
			IList<FileInfo> fileInfos = DirectoryInfoUtility.GetFiles(fileSpec, scope);

            ParsedPath[] files = new ParsedPath[fileInfos.Count];

            for (int i = 0; i < fileInfos.Count; i++)
                files[i] = new ParsedPath(fileInfos[i].FullName, PathType.File);

            return files;
		}
		
		/// <summary>
		/// See <see cref="DirectoryInfoUtility.GetDirectories(string, SearchScope)"/>
		/// </summary>
		/// <param name="dirSpec"></param>
		/// <param name="scope"></param>
		/// <returns></returns>
		public static IList<ParsedPath> GetDirectories(ParsedPath dirSpec, SearchScope scope)
		{
			IList<DirectoryInfo> dirInfos = DirectoryInfoUtility.GetDirectories(dirSpec, scope);

            ParsedPath[] dirs = new ParsedPath[dirInfos.Count];

            for (int i = 0; i < dirInfos.Count; i++)
                dirs[i] = new ParsedPath(dirInfos[i].FullName, PathType.Directory);

            return dirs;
        }
	}
}
