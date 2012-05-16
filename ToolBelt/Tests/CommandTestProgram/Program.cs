using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommandTestProgram
{
	class Program
	{
		static int Main(string[] args)
		{
			if (args.Length == 0)
				return 0;
			
			int argCount = 0;
			string stdout = String.Empty;
			string stderr = String.Empty;

			foreach (string arg in args)
			{
				if (arg.StartsWith("E:"))
				{
					stderr += arg.Substring(2);
				}
				else if (arg.StartsWith("/") || arg.StartsWith("-"))
				{
					argCount++;
				}
				else
				{
					if (stdout.Length == 0)
						stdout = " ";
					stdout += arg;
				}
			}
			
			switch (argCount)
			{
				case 0:
					Console.Write("zero");
					break;
				case 1:
					Console.Write("one");
					break;
				case 2:
					Console.Write("two");
					break;
				case 3:
					Console.Write("three");
					break;
				default:
					Console.Write("many");
					break;
			}

			Console.Write(" arguments");
			Console.WriteLine(stdout);
			
			if (stderr.Length > 0)
				Console.Error.WriteLine(stderr);

			return 1;
		}
	}
}
