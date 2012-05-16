using System;
using System.IO;
using Toaster;
using ToolBelt;

namespace ToolBelt.Tests
{
	[TestClass] 
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
		
		[ClassInitialize]
        public static void ClassInitialize(TestContext context)
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
		
		[TestMethod] 
		public void GetFilesDirOnly() 
		{
			string[] files = DirectoryUtility.GetFiles(Environment.CurrentDirectory,
				testDirs[1] + "*.txt", SearchScope.DirectoryOnly);

			Assert.IsTrue(files.Length == 1);
			Assert.IsTrue(files[0].EndsWith(testFile));
		}
		
		[TestMethod] 
		public void GetFilesSubDir() 
		{
            string[] files = DirectoryUtility.GetFiles(Environment.CurrentDirectory, 
                testDirs[0] + "*.*", SearchScope.RecurseSubDirectoriesBreadthFirst);

            Assert.IsTrue(files.Length == 10);
            Assert.IsTrue(Path.GetDirectoryName(files[files.Length - 1]).EndsWith("child221"));

            foreach (string file in files)
            {
                Assert.IsTrue(file.EndsWith(testFile));
            }

            files = DirectoryUtility.GetFiles(Environment.CurrentDirectory, 
                testDirs[0] + "*.*", SearchScope.RecurseSubDirectoriesDepthFirst);

            Assert.IsTrue(files.Length == 10);
            Assert.IsTrue(Path.GetDirectoryName(files[files.Length - 1]).EndsWith("root"));

            foreach (string file in files)
            {
                Assert.IsTrue(file.EndsWith(testFile));
            }
        }

		[TestMethod] 
		public void GetFilesParentDirs() 
		{
            FileInfo[] fileInfos = DirectoryInfoUtility.GetFiles(Environment.CurrentDirectory, 
				testDirs[7] + testFile, SearchScope.RecurseParentDirectories);

			Assert.IsTrue(fileInfos.Length == 4);
			Assert.IsTrue(fileInfos[0].FullName.EndsWith(testFile));
		}
		
		[TestMethod] 
		public void GetDirectoriesDirOnly()
		{
            DirectoryInfo[] dirInfos = DirectoryInfoUtility.GetDirectories(Environment.CurrentDirectory, testDirs[0] + "child*", 
				SearchScope.DirectoryOnly);
				
			Assert.IsTrue(dirInfos.Length == 2);
			Assert.IsTrue(dirInfos[0].FullName.EndsWith("child1"));
		}
		
		[TestMethod] 
		public void GetDirectoriesSubDirs()
		{
			// First check breadth first.  Also test the base directory parameter
            DirectoryInfo[] dirInfos = DirectoryInfoUtility.GetDirectories(Path.GetFullPath(testDirs[0] + "child1"), "child*", 
				SearchScope.RecurseSubDirectoriesBreadthFirst);
				
			Assert.IsTrue(dirInfos.Length == 4);
			Assert.IsTrue(dirInfos[0].FullName.EndsWith("child11"));
            Assert.IsTrue(dirInfos[1].FullName.EndsWith("child12"));
            Assert.IsTrue(dirInfos[2].FullName.EndsWith("child111"));
            Assert.IsTrue(dirInfos[3].FullName.EndsWith("child121"));

            // Now check depth first.
            dirInfos = DirectoryInfoUtility.GetDirectories(Path.GetFullPath(testDirs[0] + "child1"), "child*", 
                SearchScope.RecurseSubDirectoriesDepthFirst);

            Assert.IsTrue(dirInfos.Length == 4);
            Assert.IsTrue(dirInfos[0].FullName.EndsWith("child11"));
            Assert.IsTrue(dirInfos[1].FullName.EndsWith("child111"));
            Assert.IsTrue(dirInfos[2].FullName.EndsWith("child12"));
            Assert.IsTrue(dirInfos[3].FullName.EndsWith("child121"));
        }

		[TestMethod] 
		public void GetDirectoriesParentDirs() 
		{
			DirectoryInfo[] dirInfos = DirectoryInfoUtility.GetDirectories(Environment.CurrentDirectory,
				testDirs[7] + "child2", SearchScope.RecurseParentDirectories);

			Assert.IsTrue(dirInfos.Length == 1);
			Assert.IsTrue(dirInfos[0].FullName.EndsWith("child2"));
		}

		[ClassCleanup] 
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
