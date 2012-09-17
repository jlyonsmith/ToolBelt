using System;
using System.Collections.Generic;
using System.Text;

namespace Hammer
{
	class Program
	{
		static int Main(string[] args)
		{
			HammerTool tool = new HammerTool();
			
			try
			{
				tool.ProcessCommandLine(args);
				
				tool.Execute();
				return (tool.HasOutputErrors ? 1 : 0);
			}
			catch (Exception exception)
			{
				Console.WriteLine("Error: Exception: {0}", exception);
				return 1;
			}
		}
	}
}
