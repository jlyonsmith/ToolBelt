using System;
using System.Collections.Generic;
using System.Text;
using ToolBelt;

namespace Buckle
{
	class Program
	{
		static int Main(string[] args)
		{
			BuckleTool tool = new BuckleTool(new ConsoleOutputter());

			if (!((IProcessCommandLine)tool).ProcessCommandLine(args))
				return 1;

			try
			{
				tool.Execute();
				return (tool.Output.HasOutputErrors ? 1 : 0);
			}
			catch (Exception exception)
			{
				Console.WriteLine("Error: Exception: {0}", exception);
				return 1;
			}
		}
	}
}
