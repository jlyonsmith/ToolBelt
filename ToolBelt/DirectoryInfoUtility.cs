using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace ToolBelt
{
	/// <summary>
	/// Extensions to the <see cref="DirectoryInfo"/> class.
	/// </summary>
	public sealed class DirectoryInfoUtility
	{
		/// <summary>
		/// Returns a list of directories given a file search pattern.  Will also search sub-directories and 
		/// parent directories.
		/// </summary>
		/// <param name="searchPattern">Search pattern.  Can include a full or partial path and standard wildcards for the directory name.</param>
		/// <param name="scope">The scope of the search. <see cref="SearchScope"/></param>
		/// <param name="baseDir">Base directory for partially qualified file names</param>
		/// <returns>An array of <c>DirectoryInfo</c> objects for files matching the search pattern. </returns>
		public static IList<DirectoryInfo> GetDirectories(ParsedPath dirSpec, SearchScope scope)
		{
			ParsedPath rootPath = dirSpec.MakeFullPath();
		
			if (scope != SearchScope.DirectoryOnly)
			{			
				List<DirectoryInfo> dirs = new List<DirectoryInfo>();	
			
				if (scope == SearchScope.RecurseParentDirectories)
					RecursiveGetParentDirectories(rootPath, ref dirs);
				else
					RecursiveGetSubDirectories(rootPath, (scope == SearchScope.RecurseSubDirectoriesBreadthFirst), ref dirs);
				
				return dirs.ToArray();
			}
			else
			{
				return NonRecursiveGetDirectories(rootPath);
			}
		}

		private static DirectoryInfo [] NonRecursiveGetDirectories(ParsedPath rootPath)
		{
			DirectoryInfo dirInfo = new DirectoryInfo(rootPath.VolumeAndDirectory);
		
			return dirInfo.GetDirectories(rootPath.FileAndExtension);
		}
	
		private static void RecursiveGetSubDirectories(ParsedPath rootPath, bool breadthFirst, ref List<DirectoryInfo> dirs)
		{
			DirectoryInfo dirInfo = new DirectoryInfo(rootPath.VolumeAndDirectory);
			DirectoryInfo[] dirInfos = dirInfo.GetDirectories(rootPath.FileAndExtension);

            if (!breadthFirst)
            {
                foreach (DirectoryInfo subDirInfo in dirInfo.GetDirectories())
                {
                    dirs.Add(subDirInfo);

                    RecursiveGetSubDirectories(
                        new ParsedPath(subDirInfo.FullName, PathType.Directory).Append(rootPath.FileAndExtension),
                        breadthFirst,
                        ref dirs);
                }
            }
            else
            {
			    dirs.AddRange(dirInfos);
			
			    foreach (DirectoryInfo subDirInfo in dirInfo.GetDirectories())
			    {
                    RecursiveGetSubDirectories(
                        new ParsedPath(subDirInfo.FullName, PathType.Directory).Append(rootPath.FileAndExtension), 
					    breadthFirst, 
					    ref dirs);
			    }
			}
        }
	
		private static void RecursiveGetParentDirectories(ParsedPath rootPath, ref List<DirectoryInfo> dirs)
		{
			DirectoryInfo dirInfo = new DirectoryInfo(rootPath.VolumeAndDirectory);
			DirectoryInfo [] dirInfos = dirInfo.GetDirectories(rootPath.FileAndExtension);
					
			dirs.AddRange(dirInfos);
			
			if (!rootPath.IsRootDirectory)
			{
				RecursiveGetParentDirectories(rootPath.MakeParentPath(), ref dirs);
			}
		}

		/// <summary>
		/// Returns a list of files given a file search pattern.  Will also search sub-directories.
		/// </summary>
		/// <param name="searchPattern">Search pattern.  Can include a full or partial path and standard wildcards for the file name.</param>
		/// <param name="scope">The scope of the search.</param>
		/// <param name="baseDir">Base directory to use for partially qualified paths</param>
		/// <returns>An array of <c>FileInfo</c> objects for files matching the search pattern. </returns>
		public static IList<FileInfo> GetFiles(ParsedPath fileSpec, SearchScope scope)
		{
			ParsedPath rootPath = fileSpec.MakeFullPath();
		
			if (scope != SearchScope.DirectoryOnly)
			{			
				List<FileInfo> files = new List<FileInfo>();	
			
				if (scope == SearchScope.RecurseParentDirectories)
					RecursiveGetParentFiles(rootPath, ref files);
				else
					RecursiveGetSubFiles(rootPath, (scope == SearchScope.RecurseSubDirectoriesBreadthFirst), ref files);
				
				return files.ToArray();
			}
			else
			{
				return NonRecursiveGetFiles(rootPath);
			}
		}

		private static IList<FileInfo> NonRecursiveGetFiles(ParsedPath rootPath)
		{
			DirectoryInfo dirInfo = new DirectoryInfo(rootPath.VolumeAndDirectory);
		
			return dirInfo.GetFiles(rootPath.FileAndExtension);
		}
	
		private static void RecursiveGetSubFiles(ParsedPath rootPath, bool breadthFirst, ref List<FileInfo> files)
		{
			DirectoryInfo dirInfo = new DirectoryInfo(rootPath.VolumeAndDirectory);
			FileInfo [] fileInfos = dirInfo.GetFiles(rootPath.FileAndExtension);

            if (breadthFirst)
                files.AddRange(fileInfos);
            
            foreach (DirectoryInfo subDirInfo in dirInfo.GetDirectories())
            {
                RecursiveGetSubFiles(
                    new ParsedPath(subDirInfo.FullName, PathType.Directory).Append(rootPath.FileAndExtension), breadthFirst, ref files);
            }

            if (!breadthFirst)
                files.AddRange(fileInfos);
        }
	
		private static void RecursiveGetParentFiles(ParsedPath rootPath, ref List<FileInfo> files)
		{
			DirectoryInfo dirInfo = new DirectoryInfo(rootPath.VolumeAndDirectory);
			FileInfo [] fileInfos = dirInfo.GetFiles(rootPath.FileAndExtension);
					
			files.AddRange(fileInfos);
			
			if (!rootPath.IsRootDirectory) 
			{
				RecursiveGetParentFiles(rootPath.MakeParentPath().VolumeAndDirectory.Append(rootPath.FileAndExtension), ref files);
			}
		}
	}
}
