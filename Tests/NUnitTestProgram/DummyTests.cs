using System;
using ToolBelt.NUnit;

namespace NUnitTestProgram
{
    [DeploymentItem("$(SolutionDir)/NUnitTestProgram/TestFile.txt")]
    [DeploymentItem("$(SolutionDir)/NUnitTestProgram/TestFile.txt", ToFilePath="SubDir/TestFile2.txt")]
    public class DummyTests
    {
        public DummyTests()
        {
        }
    }
}

