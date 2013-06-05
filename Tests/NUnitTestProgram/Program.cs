using System;
using ToolBelt.NUnit;

namespace NUnitTestProgram
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            TestHelper.ProcessDeploymentItems(typeof(DummyTests));
        }
    }
}
