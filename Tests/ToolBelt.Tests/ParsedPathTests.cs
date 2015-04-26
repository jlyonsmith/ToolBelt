using System;
using System.IO;
using NUnit.Framework;
using ToolBelt;
using System.Collections.Generic;

namespace ToolBelt.Tests
{
    [TestFixture] 
    public class ParsedPathTests
    {
        [Test] public void TestOperators()
        {
            ParsedPath pp = new ParsedPath("file.txt", PathType.Unknown);
            
            Assert.IsFalse(pp == null);
            Assert.IsFalse(null == pp);
            Assert.IsTrue(pp != null);
            Assert.IsTrue(null != pp);
            Assert.IsFalse(pp.Equals(null));
            Assert.IsFalse(ParsedPath.Equals(pp, null));
            Assert.IsFalse(ParsedPath.Equals(null, pp));
            
            Assert.AreNotEqual(0, pp.GetHashCode());
            
            Assert.IsFalse(pp == ParsedPath.Empty);
        }

        [Test]
        public void TestCompareTo()
        {
            ParsedPath ppA = new ParsedPath(@"c:\blah\a.txt", PathType.File);
            ParsedPath ppB = new ParsedPath(@"c:\blah\a.txt", PathType.File);
            ParsedPath ppB2 = new ParsedPath(@"c:\blah\b.txt", PathType.File);
            ParsedPath ppC = new ParsedPath(@"c:\blah\a.txt", PathType.File);
            ParsedPath ppC2 = new ParsedPath(@"c:\blah\b.txt", PathType.File);
#if MACOS
            string p = @"c:/blah/a.txt";
#else
            string p = @"c:\blah\a.txt";
#endif

            // MSDN gives the following axioms for CompareTo()
            Assert.AreEqual(0, ppA.CompareTo(ppB));
            Assert.AreEqual(0, ppB.CompareTo(ppA));
            Assert.AreEqual(0, ppB.CompareTo(ppC));
            Assert.AreEqual(0, ppA.CompareTo(ppC));
            int x = ppA.CompareTo(ppB2);
            Assert.IsTrue(x != 0);
            Assert.IsTrue(Math.Sign(x) == -Math.Sign(ppB2.CompareTo(ppA)));
            int y = ppB.CompareTo(ppC2);
            Assert.IsTrue(Math.Sign(x) == Math.Sign(y));
            Assert.AreEqual(Math.Sign(x), ppA.CompareTo(ppC2));

            // Need to be able to compare ParsePath to String
            Assert.AreEqual(0, ppA.CompareTo(p));
        }

        [Test] public void ConstructParsedPath() 
        {
            // Test some good paths
            AssertPathParts(@"\", PathType.Unknown, "", "", "", @"\", "", "");
            AssertPathParts(@".txt", PathType.Unknown, "", "", "", "", "", ".txt");
            AssertPathParts(@"*.txt", PathType.File, "", "", "", "", @"*", @".txt");
            AssertPathParts(@"..\*.txt", PathType.Unknown, "", "", "", @"..\", @"*", @".txt");
            AssertPathParts(@".", PathType.Unknown, "", "", "", "", "", "");
            AssertPathParts(@".", PathType.Directory, "", "", "", @".\", "", "");
            AssertPathParts(@"..", PathType.Unknown, "", "", "", "", "", "");
            AssertPathParts(@"..", PathType.Directory, "", "", "", @"..\", "", "");
            
            AssertPathParts(@"c:", PathType.Unknown, "", "", @"c:", "", "", "");
            AssertPathParts(@"c:", PathType.Volume, "", "", @"c:", "", "", "");
            AssertPathParts(@"c:\", PathType.Directory, "", "", @"c:", @"\", "", "");
            AssertPathParts(@"c:foo.txt", PathType.Unknown, "", "", @"c:", "", "foo", ".txt");
            AssertPathParts(@"c:.txt", PathType.Unknown, "", "", @"c:", "", "", ".txt");
            AssertPathParts(@"c:....txt", PathType.Unknown, "", "", @"c:", "", "...", ".txt");
            AssertPathParts(@"c:foo.txt", PathType.Directory, "", "", @"c:", @"foo.txt\", "", "");
            AssertPathParts(@"c:blah\blah\", PathType.Unknown, "", "", @"c:", @"blah\blah\", "", "");
            AssertPathParts(@"c:blah\blah", PathType.Directory, "", "", @"c:", @"blah\blah\", "", "");
            AssertPathParts(@"c:blah\blah", PathType.Unknown, "", "", @"c:", @"blah\", "blah", "");
            AssertPathParts(@"c:\test", PathType.Directory, "", "", @"c:", @"\test\", @"", "");
            AssertPathParts(@"c:\test", PathType.File, "", "", @"c:", @"\", @"test", "");
            AssertPathParts(@"c:\test.txt", PathType.File, "", "", @"c:", @"\", @"test", @".txt");
            AssertPathParts(@"c:\whatever\test.txt", PathType.File, "", "", @"c:", @"\whatever\", @"test", @".txt");
            AssertPathParts(@"c:\test\..\temp\??.txt", PathType.File, "", "", @"c:", @"\test\..\temp\", @"??", @".txt");
            AssertPathParts(@"C:/test\the/use of\forward/slashes\a.txt", PathType.Unknown, 
                "", "", @"C:", @"\test\the\use of\forward\slashes\", "a", @".txt");
            AssertPathParts(@"   C:\ remove \  trailing \     spaces   \  file.txt   ", PathType.Unknown, 
                "", "", @"C:", @"\ remove\  trailing\     spaces\", "  file", @".txt");
            AssertPathParts(@"C:\remove.\trailing....\dots . .\and\spaces. . \file.txt. . .", PathType.Unknown, 
                "", "", @"C:", @"\remove\trailing\dots\and\spaces\", "file", @".txt");
            AssertPathParts(@"  .  . a . really . strange . name .. . . \a\b\c\...", PathType.Directory, 
                "", "", "", @".  . a . really . strange . name\a\b\c\...\", "", "");
            AssertPathParts(@"  ""  C:\this path is quoted\file.txt  ""  ", PathType.Unknown, 
                "", "", @"C:", @"\this path is quoted\", "file", @".txt");
            AssertPathParts(@"c:\assembly\Company.Product.Subcomponent.dll", PathType.Unknown, 
                "", "", @"c:", @"\assembly\", "Company.Product.Subcomponent", @".dll");
            
            AssertPathParts(@"\\machine\share", PathType.Unknown, @"\\machine", @"\share", "", "", "", "");
            AssertPathParts(@"\\machine\share\", PathType.Unknown, @"\\machine", @"\share", "", @"\", @"", @"");
            AssertPathParts(@"\\computer\share\test\subtest\abc.txt", PathType.Unknown, 
                @"\\computer", @"\share", "", @"\test\subtest\", @"abc", @".txt");

            // Test special last folder property
            Assert.AreEqual("c", new ParsedPath(@"c:\a\b\c\", PathType.Unknown).LastDirectoryNoSeparator);
#if MACOS
            Assert.AreEqual(@"/", new ParsedPath(@"c:\", PathType.Unknown).LastDirectoryNoSeparator);
#else
            Assert.AreEqual(@"\", new ParsedPath(@"c:\", PathType.Unknown).LastDirectoryNoSeparator);
#endif

            // Test some bad paths
            AssertBadPath(@"", PathType.File);
            AssertBadPath(@":", PathType.File);
            AssertBadPath(@"::::a", PathType.File);
            AssertBadPath(@"\", PathType.File);
            AssertBadPath(@"\  \", PathType.Directory);
            AssertBadPath(@"  ""    ""  ", PathType.Unknown);
            AssertBadPath(@"c:\*&#()_@\~{}|<>", PathType.Unknown);

            AssertBadPath(@"c:", PathType.Directory);
            AssertBadPath(@"c: \file.txt", PathType.File);
            AssertBadPath(@"c:\a\b\*\c\", PathType.Unknown);
            AssertBadPath(@"c:\dir\file.txt", PathType.Volume);

            AssertBadPath(@"\\", PathType.Volume);
            AssertBadPath(@"\\computer", PathType.Volume);
            AssertBadPath(@"\\computer\", PathType.Volume);
            AssertBadPath(@"\\computer\\", PathType.Volume);
        }

        [Test] public void MakeFullPath()
        {
            // Test some good paths
#if MACOS
            AssertPathPartsFull(@".txt", 
                                @"/temp/", "", "", "", @"\temp\", "", ".txt");
            AssertPathPartsFull(@"/test/../temp/??.txt", 
                                null, "", "", "", @"\temp\", @"??", @".txt");
            AssertPathPartsFull(@"\test\..\temp\abc.txt", 
                                @"\a\b\", "", "", "", @"\temp\", @"abc", @".txt");
            AssertPathPartsFull(@"test\..\temp\abc.txt", 
                                @"\a\b\", "", "", "", @"\a\b\temp\", @"abc", @".txt");
            AssertPathPartsFull(@".\test\....\temp\abc.txt", 
                                @"\a\b\c\", "", "", "", @"\a\temp\", @"abc", @".txt");
            AssertPathPartsFull(@"...\test\abc.txt", 
                                @"\a\b\c\", "", "", "", @"\a\test\", @"abc", @".txt");
            AssertPathPartsFull(@"C:\temp\..yes...this...is.a..legal.file.name....\and...\so...\...is\.this.\blah.txt", 
                                null, "", "", @"C:", 
                                @"\temp\..yes...this...is.a..legal.file.name\and\so\...is\.this\", 
                                @"blah", @".txt");
#else
            AssertPathPartsFull(@".txt", 
                @"c:\temp\", "", "", @"c:", @"\temp\", "", ".txt");
            AssertPathPartsFull(@"c:\test\..\temp\??.txt", 
                null, "", "", @"c:", @"\temp\", @"??", @".txt");
            AssertPathPartsFull(@"\test\..\temp\abc.txt", 
                @"c:\a\b\", "", "", "c:", @"\temp\", @"abc", @".txt");
            AssertPathPartsFull(@"test\..\temp\abc.txt", 
                @"c:\a\b\", "", "", @"c:", @"\a\b\temp\", @"abc", @".txt");
            AssertPathPartsFull(@".\test\....\temp\abc.txt", 
                @"c:\a\b\c\", "", "", "c:", @"\a\temp\", @"abc", @".txt");
            AssertPathPartsFull(@"...\test\abc.txt", 
                @"c:\a\b\c\", "", "", "c:", @"\a\test\", @"abc", @".txt");
            AssertPathPartsFull(@"C:\temp\..yes...this...is.a..legal.file.name....\and...\so...\...is\.this.\blah.txt", 
                null, "", "", @"C:", 
                @"\temp\..yes...this...is.a..legal.file.name\and\so\...is\.this\", 
                @"blah", @".txt");
#endif
            // Test that using the current directory works
            ParsedPath pp = new ParsedPath(Environment.CurrentDirectory, PathType.Directory);
            AssertPathPartsFull(@"test.txt", null, pp.Machine, pp.Share, pp.Drive, pp.Directory, "test", ".txt");

            // Test some bad paths
            AssertBadPathFull(@"c:\test\..\..\temp\abc.txt", null);  // Too many '..'s
            AssertBadPathFull(@"test\......\temp\.\abc.txt", @"c:\");  // Too many '....'s
        }
        
        [Test] public void MakeRelativePath()
        {
            AssertPathPartsRelative(@"c:\a\p.q", @"c:\a\", @"./");
            AssertPathPartsRelative(@"c:\a\", @"c:\a\b\c\p.q", @"../../"); 
            AssertPathPartsRelative(@"c:\a\b\c\p.q", @"c:\a\", @"./b/c/"); 
            AssertPathPartsRelative(@"c:\a\b\c\p.q", @"c:\a\x\y\", @"../../b/c/");
            AssertPathPartsRelative(@"/a/b/c.e", @"/a/b/x/y/z.1", @"../../");

            AssertBadPathPartsRelative(@"..\a.txt", @"c:/a/b");
            AssertBadPathPartsRelative(@"a.txt", @"b");
        }

        [Test] public void MakeParentPath()
        {
            // Test going up one parent
            AssertParentPath(@"c:\a\b\c\p.q", -1, "", "", "c:", @"\a\b\", "p", ".q"); 
            AssertParentPath(@"c:\a\b\c\", -1, "", "", "c:", @"\a\b\", "", ""); 
            AssertParentPath(@"\\machine\share\a\b\c\d\foo.bar", -1, 
                @"\\machine", @"\share", "", @"\a\b\c\", "foo", ".bar"); 
            
            // Test going up multiple parents
            AssertParentPath(@"c:\a\b\c\d\e\", -3, "", "", "c:", @"\a\b\", "", "");
            AssertParentPath(@"c:\a\b\c\d\e\", -5, "", "", "c:", @"\", "", "");

            // Test bad stuff
            AssertBadParentPath(@"c:\", -1); // Already root
            AssertBadParentPath(@"c:\a\", 2); // Positive index not allowed
            AssertBadParentPath(@"c:\a\b\c\", -4); // Too many parent levels
            AssertBadParentPath(@"c:\a\b\..\c\", -1); // Path cannot be relative 
        }

        #region Assert paths parts
        public static void AssertPathParts(
            string path,
            PathType type, 
            string machine,
            string share,
            string drive,
            string directory,
            string file,
            string extension)
        {
            ParsedPath pp = new ParsedPath(path, type);
        
            #if MACOS
            // We have to compare to OS specific paths
            machine = machine.Replace(@"\", "/");
            share = share.Replace(@"\", "/");
            directory = directory.Replace(@"\", "/");
            #endif

            Assert.AreEqual(machine, pp.Machine.ToString());
            Assert.AreEqual(share, pp.Share.ToString());
            Assert.AreEqual(drive, pp.Drive.ToString());
            Assert.AreEqual(directory, pp.Directory.ToString());
            Assert.AreEqual(file, pp.File.ToString());
            Assert.AreEqual(extension, pp.Extension.ToString());
        }
        
        public static void AssertBadPath(
            string path, 
            PathType type)
        {
            try
            {
                ParsedPath pp = new ParsedPath(path, type);
                Assert.IsNotNull(pp);
                Assert.Fail("Badly formed path not caught");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentException);
            }
        }
        #endregion      

        #region Assert path parts with MakeFullPath()
        public static void AssertPathPartsFull(
            string path,
            string baseDir,
            string machine,
            string share,
            string drive,
            string directory,
            string file,
            string extension)
        {
            ParsedPath pp;
            
            if (baseDir != null)
                pp = new ParsedPath(path, PathType.Unknown).MakeFullPath(new ParsedPath(baseDir, PathType.Directory));
            else
                pp = new ParsedPath(path, PathType.Unknown).MakeFullPath();
        
            #if MACOS
            // We have to compare to OS specific paths
            machine = machine.Replace(@"\", "/");
            share = share.Replace(@"\", "/");
            directory = directory.Replace(@"\", "/");
            #endif

            Assert.AreEqual(machine, pp.Machine.ToString());
            Assert.AreEqual(share, pp.Share.ToString());
            Assert.AreEqual(drive, pp.Drive.ToString());
            Assert.AreEqual(directory, pp.Directory.ToString());
            Assert.AreEqual(file, pp.File.ToString());
            Assert.AreEqual(extension, pp.Extension.ToString());
        }
        
        public static void AssertBadPathFull(
            string path, 
            string baseDir)
        {
            try
            {
                ParsedPath pp = new ParsedPath(path, PathType.Unknown).MakeFullPath(
                    baseDir == null ? null : new ParsedPath(baseDir, PathType.Directory));
                Assert.IsNotNull(pp);
                Assert.Fail("Badly formed path not caught");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentException);
            }
        }
        #endregion

        #region Assert path directory with MakeRelativePath()
        public static void AssertPathPartsRelative(
            string path,
            string basePath,
            string directory)
        {
            ParsedPath pp = new ParsedPath(path, PathType.Unknown).MakeRelativePath(new ParsedPath(basePath, PathType.Unknown));
            Assert.AreEqual(directory, pp.Directory.ToString());
        }

        private void AssertBadPathPartsRelative(
            string path,
            string basePath)
        {
            try
            {
                ParsedPath pp = new ParsedPath(path, PathType.Unknown).MakeRelativePath(new ParsedPath(basePath, PathType.Unknown));
                Assert.IsNotNull(pp);
                Assert.Fail("MakeRelativePath succeeded and should have failed");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentException);
            }
        }
        #endregion

        #region Assert path directory with MakeParentPath()
        public static void AssertParentPath(
            string path,
            int level,
            string machine,
            string share,
            string drive,
            string directory,
            string file,
            string extension)
        {
            ParsedPath pp;
            
            // Test out specific entry points based on the values passed in
            if (level < -1)
            {
                pp = new ParsedPath(path, PathType.Unknown).MakeParentPath(level);
            
                if (pp == null)
                {
                    Assert.IsNull(directory, "Expected result was not null");
                    return;
                }
            }
            else
            {
                pp = new ParsedPath(path, PathType.Unknown).MakeParentPath();

                if (pp == null)
                {
                    Assert.IsNull(directory, "Expected result was not null");
                    return;
                }
            }
                    
            #if MACOS
            // We have to compare to OS specific paths
            machine = machine.Replace(@"\", "/");
            share = share.Replace(@"\", "/");
            directory = directory.Replace(@"\", "/");
            #endif

            Assert.AreEqual(machine, pp.Machine.ToString());
            Assert.AreEqual(share, pp.Share.ToString());
            Assert.AreEqual(drive, pp.Drive.ToString());
            Assert.AreEqual(directory, pp.Directory.ToString());
            Assert.AreEqual(file, pp.File.ToString());
            Assert.AreEqual(extension, pp.Extension.ToString());
        }

        public static void AssertBadParentPath(
            string path,
            int level)
        {
            try
            {
                ParsedPath pp;
                
                if (level <= -1 || level > 0)
                    pp = new ParsedPath(path, PathType.Unknown).MakeParentPath(level);
                else
                    pp = new ParsedPath(path, PathType.Unknown).MakeParentPath();
                
                Assert.IsNotNull(pp);
                Assert.Fail("Get parent succeeded and should have failed");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentException || e is InvalidOperationException);
            }
        }
        #endregion

        [Test] public void TestPathTypes()
        {
            Assert.IsTrue(new ParsedPath(@"c:\temp\", PathType.Unknown).IsDirectory);
            Assert.IsFalse(new ParsedPath(@"c:\temp", PathType.Unknown).IsDirectory);
            Assert.IsTrue(new ParsedPath(@"\\machine\share\foo", PathType.Unknown).HasUnc);
            Assert.IsFalse(new ParsedPath(@"c:\foo", PathType.Unknown).HasUnc);
            Assert.IsTrue(new ParsedPath(@"\\machine\share\foo\*.t?t", PathType.Unknown).HasWildcards);
            Assert.IsFalse(new ParsedPath(@"\\machine\share\foo\foo.txt", PathType.Unknown).HasWildcards);
            Assert.IsTrue(new ParsedPath(@"\\machine\share\foo\test.txt", PathType.Unknown).HasVolume);
            Assert.IsFalse(new ParsedPath(@"foo\test.txt", PathType.Unknown).HasVolume);
            Assert.IsTrue(new ParsedPath(@"C:\share\foo\", PathType.Unknown).HasDrive);
            Assert.IsFalse(new ParsedPath(@"\\machine\share\foo\", PathType.Unknown).HasDrive);
            Assert.IsTrue(new ParsedPath(@"C:\share\foo\..\..\thing.txt", PathType.Unknown).IsRelativePath);
            Assert.IsTrue(new ParsedPath(@"C:\share\foo\...\thing.txt", PathType.Unknown).IsRelativePath);
            Assert.IsTrue(new ParsedPath(@"...\thing.txt", PathType.Unknown).IsRelativePath);
            Assert.IsFalse(new ParsedPath(@"\\machine\share\foo\thing.txt", PathType.Unknown).IsRelativePath);
            Assert.IsFalse(new ParsedPath(@"C:\share\foo\thing.txt", PathType.Unknown).IsRelativePath);
            Assert.IsFalse(new ParsedPath(@"/home/thing.txt", PathType.Unknown).IsRelativePath);
        }

        [Test] public void TestSubDirectories()
        {
            Assert.AreEqual(4, new ParsedPath(@"c:\a\b\c\", PathType.Unknown).SubDirectories.Count);
            Assert.AreEqual(4, new ParsedPath(@"\\machine\share\a\b\c\", PathType.Unknown).SubDirectories.Count);
            
            IList<ParsedPath> subDirs;
            
            subDirs = new ParsedPath(@"c:\a\b\c\", PathType.Directory).SubDirectories;
            
            Assert.AreEqual(4, subDirs.Count);
            Assert.AreEqual(Path.DirectorySeparatorChar.ToString(), subDirs[0].ToString());
            Assert.AreEqual("c" + PathUtility.DirectorySeparator, subDirs[3].ToString());
            
            subDirs = new ParsedPath(@"c:\", PathType.Directory).SubDirectories;
            
            Assert.AreEqual(1, subDirs.Count);
            Assert.AreEqual(PathUtility.DirectorySeparator, subDirs[0].ToString());
        }

        [Test] public void TestAppend()
        {
            ParsedPath pp1 = new ParsedPath(@"c:\blah\blah", PathType.Directory);
            ParsedPath ppCombine = pp1.Append("file.txt", PathType.File);

#if MACOS
            Assert.AreEqual(@"c:/blah/blah/file.txt", ppCombine.ToString());
#else
            Assert.AreEqual(@"c:\blah\blah\file.txt", ppCombine.ToString());
#endif

            pp1 = new ParsedPath(@"c:\blah\blah", PathType.Directory);
            Assert.Throws(typeof(ArgumentException), delegate { ppCombine = pp1.Append(@"\blah\file.txt", PathType.File); });

            pp1 = new ParsedPath(@"c:\blah\blah", PathType.Directory);
            ppCombine = pp1.Append(@"blah\file.txt", PathType.File);

#if MACOS
            Assert.AreEqual(@"c:/blah/blah/blah/file.txt", ppCombine.ToString());
#else
            Assert.AreEqual(@"c:\blah\blah\blah\file.txt", ppCombine.ToString());
#endif
        }
    }
}
