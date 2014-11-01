using System;
using NUnit.Framework;

namespace ToolBelt.Tests
{
    [TestFixture] 
    public class ParsedDirectoryPathTests
    {
        [Test] public void TestGoodDirectoryPaths()
        {
            AssertDirectoryPathParts("/blah/", "", "", "", "/blah/");
            AssertDirectoryPathParts("/", "", "", "", "/");
            AssertDirectoryPathParts("lookslikeafile.txt", "", "", "", "lookslikeafile.txt/");
        }

        [Test] public void TestBadDirectoryPaths()
        {
        }

        [Test] public void TestGoodConversion()
        {
            var path = new ParsedDirectoryPath(new ParsedPath("a/b", PathType.Directory));

            AssertDirectoryPathParts(path, "", "", "", "a/b/");
        }

        [Test] public void TestBadConversion()
        {
            Assert.Throws<InvalidCastException>(() => new ParsedDirectoryPath(new ParsedPath("a.txt", PathType.File)));
        }

        public static void AssertDirectoryPathParts(
            string path,
            string machine,
            string share,
            string drive,
            string directory)
        {
            ParsedDirectoryPath pp = new ParsedDirectoryPath(path);

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
        }
    }
}

