using System;
using System.IO;
using ToolBelt;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace ToolBelt.Tests
{
	[TestFixture] 
	public class DirectoryUtilityTests
	{
        // Each test directory should end in '\'
		static string[] testDirs = 
		{
			@"root\",                           // 0
			@"root\child1\",                    // 1
			@"root\child1\child11\",            // 2
			@"root\child1\child11\child111\",   // 3
			@"root\child1\child12\",            // 4
			@"root\child1\child12\child121\",   // 5
			@"root\child2\",                    // 6
			@"root\child2\child21\",            // 7
			@"root\child2\child22\",            // 8
			@"root\child2\child22\child221\"    // 9
		};
		
		// Let's hope this file never appears in any of your parent directories!
		static string testFile = @"~GetFilesTestFile.txt";
		
		[TestFixtureSetUp]
        public static void TestFixtureSetUp()
		{
			try
			{			
				foreach (string dir in testDirs)
				{
					Directory.CreateDirectory(dir);
						
					// Put a file in each of the directories	
					using (StreamWriter wr = File.CreateText(dir + testFile))
					{
						wr.WriteLine("abc");
					}
				}

				// Create a test file in the current directory	
				using (StreamWriter wr = File.CreateText(testFile))
				{
					wr.WriteLine("abc");
				}
			}
			catch
			{
				Assert.Fail("Could not create test directories/files");
			}
		}
		
		[TestCase] 
		public void GetFilesDirOnly() 
		{
			IList<ParsedPath> files = DirectoryUtility.GetFiles(
				new ParsedPath(testDirs[1] + "*.txt", PathType.File), SearchScope.DirectoryOnly);

			Assert.AreEqual(1, files.Count);
			Assert.AreEqual(files[0].FileAndExtension, testFile);
		}
		
		[TestCase] 
		public void GetFilesSubDir() 
		{
            IList<ParsedPath> files = DirectoryUtility.GetFiles( 
                new ParsedPath(testDirs[0] + "*.*", PathType.File), SearchScope.RecurseSubDirectoriesBreadthFirst);

            Assert.AreEqual(10, files.Count);
            Assert.AreEqual(files[files.Count - 1].SubDirectories.Last(), "child221");

            foreach (string file in files)
            {
                Assert.IsTrue(file.EndsWith(testFile));
            }

            files = DirectoryUtility.GetFiles(
                new ParsedPath(testDirs[0] + "*.*", PathType.File), SearchScope.RecurseSubDirectoriesDepthFirst);

            Assert.AreEqual(10, files.Count);
            Assert.IsTrue(Path.GetDirectoryName(files[files.Count - 1]).EndsWith("root"));

            foreach (string file in files)
            {
                Assert.IsTrue(file.EndsWith(testFile));
            }
        }

		[TestCase] 
		public void GetFilesParentDirs() 
		{
            IList<FileInfo> fileInfos = DirectoryInfoUtility.GetFiles( 
				new ParsedPath(testDirs[7] + testFile, PathType.File), SearchScope.RecurseParentDirectories);

			Assert.AreEqual(4, fileInfos.Count);
			Assert.AreEqual(testFile, fileInfos[0].FullName);
		}
		
		[TestCase] 
		public void GetDirectoriesDirOnly()
		{
            IList<DirectoryInfo> dirInfos = DirectoryInfoUtility.GetDirectories(
				new ParsedPath(testDirs[0] + "child*", PathType.Directory), 
				SearchScope.DirectoryOnly);
				
			Assert.AreEqual(2, dirInfos.Count);
			Assert.IsTrue(dirInfos[0].FullName.EndsWith("child1"));
		}
		
		[TestCase] 
		public void GetDirectoriesSubDirs()
		{
			// First check breadth first.  Also test the base directory parameter
            IList<DirectoryInfo> dirInfos = DirectoryInfoUtility.GetDirectories(
				new ParsedPath(testDirs[0] + "child*", PathType.Directory).MakeFullPath(), 
				SearchScope.RecurseSubDirectoriesBreadthFirst);
				
			Assert.AreEqual(4, dirInfos.Count);
			Assert.IsTrue(dirInfos[0].FullName.EndsWith("child11"));
            Assert.IsTrue(dirInfos[1].FullName.EndsWith("child12"));
            Assert.IsTrue(dirInfos[2].FullName.EndsWith("child111"));
            Assert.IsTrue(dirInfos[3].FullName.EndsWith("child121"));

            // Now check depth first.
            dirInfos = DirectoryInfoUtility.GetDirectories(
				new ParsedPath(testDirs[0] + "child*", PathType.Directory).MakeFullPath(), 
                SearchScope.RecurseSubDirectoriesDepthFirst);

            Assert.AreEqual(4, dirInfos.Count);
            Assert.IsTrue(dirInfos[0].FullName.EndsWith("child11"));
            Assert.IsTrue(dirInfos[1].FullName.EndsWith("child111"));
            Assert.IsTrue(dirInfos[2].FullName.EndsWith("child12"));
            Assert.IsTrue(dirInfos[3].FullName.EndsWith("child121"));
        }

		[TestCase] 
		public void GetDirectoriesParentDirs() 
		{
			IList<DirectoryInfo> dirInfos = DirectoryInfoUtility.GetDirectories(
				new ParsedPath(testDirs[7] + "child2", PathType.Directory), 
				SearchScope.RecurseParentDirectories);

			Assert.AreEqual(1, dirInfos.Count);
			Assert.IsTrue(dirInfos[0].FullName.EndsWith("child2"));
		}

		[TestFixtureTearDown] 
		public static void ClassCleanup()
		{
			try 
			{
				Directory.Delete("root", true);
				File.Delete(testFile);
			}
			catch
			{
				Assert.Fail("Unable to clean-up directories");
			}
		}
	}
}
