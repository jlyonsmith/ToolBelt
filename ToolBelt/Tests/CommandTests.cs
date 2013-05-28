using System;
using System.IO;
using ToolBelt;
using NUnit.Framework;

namespace ToolBelt.Tests
{
    [TestFixture]
    // TODO: How to do in NUnit
    //[(@"$(SolutionRoot)\ToolBelt\Tests\CommandTestProgram\bin\$(Configuration)\CommandTestProgram.exe")]
    public class CommandTests 
    {
        [TestCase]
        public void TestNoCapture() 
        {
            Assert.IsTrue(Command.Run("dir /b") == 0);
        }

        [TestCase]
        public void TestCaptureOutput() 
        {
            string output;

            Assert.IsTrue(Command.Run("CommandTestProgram.exe /1", out output) != 0);
            Assert.IsTrue(output == "one arguments\r\n");
        }

        [TestCase]
        public void TestCaptureOutputAndError() 
        {
            string output;
            string error;

            Assert.IsTrue(Command.Run("CommandTestProgram.exe /1 /2 \"E:error text\"", out output, out error) != 0);
            Assert.IsTrue(output == "two arguments\r\n");
            Assert.IsTrue(error == "error text\r\n");
        }

        [TestCase]
        public void TestQuotedContent() 
        {
            string output;
            
            Assert.IsTrue(Command.Run("CommandTestProgram.exe /1 /2 \"plus quoted string\"", out output) != 0);
            Assert.IsTrue(output == "two arguments plus quoted string\r\n");
        }

        [TestCase]
        public void TestNonZeroExitCode()
        {
            string output;

            Assert.IsTrue(Command.Run("CommandTestProgram.exe /1 /2 /3 /4 /5", out output) != 0);
        }

        [TestCase]
        public void TestDebugMode()
        {
            string output;

            Command.DebugMode = true;
            Assert.IsTrue(Command.DebugMode);
            Assert.AreEqual(0, Command.Run("CommandTestProgram.exe", out output));
            Assert.AreEqual("CommandTestProgram.exe\r\n", output);
        }
        
        [TestFixtureTearDown]
        public void TestCleanup()
        {
            Command.DebugMode = false;
        }
    }
}
