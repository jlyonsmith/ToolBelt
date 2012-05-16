using System;
using System.IO;
using Toaster;
using ToolBelt;

namespace ToolBelt.Tests
{
	[TestClass]
    [DeploymentItem(@"$(SolutionRoot)\ToolBelt\Tests\CommandTestProgram\bin\$(Configuration)\CommandTestProgram.exe")]
	public class CommandTests 
	{
		[TestMethod]
		public void TestNoCapture() 
		{
			Assert.IsTrue(Command.Run("dir /b") == 0);
		}

		[TestMethod]
		public void TestCaptureOutput() 
		{
			string output;

			Assert.IsTrue(Command.Run("CommandTestProgram.exe /1", out output) != 0);
			Assert.IsTrue(output == "one arguments\r\n");
		}

		[TestMethod]
		public void TestCaptureOutputAndError() 
		{
			string output;
			string error;

			Assert.IsTrue(Command.Run("CommandTestProgram.exe /1 /2 \"E:error text\"", out output, out error) != 0);
			Assert.IsTrue(output == "two arguments\r\n");
			Assert.IsTrue(error == "error text\r\n");
		}

		[TestMethod]
		public void TestQuotedContent() 
		{
			string output;
			
			Assert.IsTrue(Command.Run("CommandTestProgram.exe /1 /2 \"plus quoted string\"", out output) != 0);
			Assert.IsTrue(output == "two arguments plus quoted string\r\n");
		}

		[TestMethod]
		public void TestNonZeroExitCode()
		{
			string output;

			Assert.IsTrue(Command.Run("CommandTestProgram.exe /1 /2 /3 /4 /5", out output) != 0);
		}

		[TestMethod]
		public void TestDebugMode()
		{
			string output;

			Command.DebugMode = true;
			Assert.IsTrue(Command.DebugMode);
			Assert.AreEqual<int>(Command.Run("CommandTestProgram.exe", out output), 0);
			Assert.AreEqual(output, "CommandTestProgram.exe\r\n");
		}
		
		[TestCleanup]
		public void TestCleanup()
		{
			Command.DebugMode = false;
		}
	}
}
