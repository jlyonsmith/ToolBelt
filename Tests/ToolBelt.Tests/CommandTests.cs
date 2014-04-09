using System;
using System.IO;
using ToolBelt;
using NUnit.Framework;
using ToolBelt.NUnit;

namespace ToolBelt
{
    [TestFixture]
    [DeploymentItem(@"$(SolutionDir)\Tests\CommandTestProgram\bin\$(Configuration)\CommandTestProgram.exe")]
    public class CommandTests 
    {
#if MACOS
        private readonly string program = "mono CommandTestProgram.exe";
#else 
        private readonly string program = "CommandTestProgram.exe";
#endif
        [TestFixtureSetUp]
        public void Setup()
        {
            TestHelper.ProcessDeploymentItems(this.GetType());
        }

        [Test]
        public void TestNoCapture() 
        {
#if MACOS
            string shellCmd = "ls -1";
#else
            string shellCmd = "dir /b";
#endif
            Assert.IsTrue(Command.Run(shellCmd) == 0);
        }

        [Test]
        public void TestCaptureOutput() 
        {
            string output;

            Assert.IsTrue(Command.Run(program + " /1", out output) != 0);
            Assert.IsTrue(output == "one arguments" + Environment.NewLine);
        }

        [Test]
        public void TestCaptureOutputAndError() 
        {
            string output;
            string error;

            Assert.IsTrue(Command.Run(program + " /1 /2 \"E:error text\"", out output, out error) != 0);
            Assert.IsTrue(output == "two arguments" + Environment.NewLine);
            Assert.IsTrue(error == "error text" + Environment.NewLine);
        }

        [Test]
        public void TestQuotedContent() 
        {
            string output;
            int exitCode = Command.Run(program + " /1 /2 \"plus quoted string\"", out output);

            Assert.IsTrue(exitCode != 0);
            Assert.IsTrue(output == "two arguments plus quoted string" + Environment.NewLine);
        }

        [Test]
        public void TestNonZeroExitCode()
        {
            string output;
            int exitCode = Command.Run(program + " /1 -2 /3 -4 /5", out output);

            Assert.IsTrue(exitCode != 0);
        }

        [Test]
        public void TestDebugMode()
        {
            string output;

            Assert.AreEqual(0, Command.Run(program, out output, true));
            Assert.AreEqual(program + Environment.NewLine, output);
        }
        
        [TestFixtureTearDown]
        public void TestCleanup()
        {
        }
    }
}
