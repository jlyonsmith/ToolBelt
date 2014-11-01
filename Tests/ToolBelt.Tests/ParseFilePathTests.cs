using System;
using NUnit.Framework;

namespace ToolBelt.Tests
{
    [TestFixture] 
    public class ParsedFilePathTests
    {
        [Test] public void TestGoodFilePaths()
        {
            AssertFilePathParts("/blah/blah.txt", "", "", "", "/blah/", "blah", ".txt");
            AssertFilePathParts("blah.txt", "", "", "", "", "blah", ".txt");
        }

        [Test] public void TestBadFilePaths()
        {
            AssertBadFilePath("/blah/");
        }

        [Test] public void TestGoodConversion()
        {
            var path = new ParsedFilePath("a/b.txt");

            path = new ParsedFilePath(path.MakeFullPath(new ParsedDirectoryPath("/x/y")));

            AssertFilePathParts(path, "", "", "", "/x/y/a/", "b", ".txt");
        }

        [Test] public void TestBadConversion()
        {
            Assert.Throws<InvalidCastException>(() => new ParsedFilePath(new ParsedPath("/x/y", PathType.Directory)));
        }

        public static void AssertFilePathParts(
            string path,
            string machine,
            string share,
            string drive,
            string directory,
            string file,
            string extension)
        {
            ParsedFilePath pp = new ParsedFilePath(path);

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

        public static void AssertBadFilePath(string path)
        {
            try
            {
                ParsedFilePath pp = new ParsedFilePath(path);
                Assert.IsNotNull(pp);
                Assert.Fail("Badly formed path not caught");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentException);
            }
        }
    }
}

