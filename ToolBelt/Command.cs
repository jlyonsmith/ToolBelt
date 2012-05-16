using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Threading;
using ToolBelt;

namespace ToolBelt
{
	/// <summary>
	/// Class for running command line tools
	/// </summary>
	public static class Command
	{
		#region Private Methods
		private static string CreateBatchFile(string command)
		{
			string batchContents = String.Format("@echo off\r\n{0}\r\n", command);
			ParsedPath batchFile = new ParsedPath(Path.GetTempFileName(), PathType.File).SetExtension(".bat");

			File.WriteAllText(batchFile, batchContents);

			if (debugMode)
			{
				Console.WriteLine(batchFile + ":");
				Console.WriteLine(batchContents);
			}

			return batchFile;
		}

		#endregion

		#region Public Methods
		/// <summary>
		/// Run the specified command through the shell specified in the COMSPEC
		/// environment variable.  Output is sent to the default console output and error streams.
		/// </summary>
		/// <param name="command">The command to pass to the shell.</param>
		/// <returns>The exit code of the process.</returns>
		public static int Run(string command)
		{
			return Run(command, Console.Out, Console.Error);  // DO NOT change this behavior!
		}

		/// <summary>
		/// Run the specified command through the shell specified in the COMSPEC 
		/// environment variable.
		/// </summary>
		/// <param name="command">The command to pass to the shell.</param>
		/// <param name="output">Interleaved results from standard output and standard error streams.</param>
		/// <returns>The exit code of the process.</returns>
		public static int Run(string command, out string output)
		{
			StringBuilder outputString = new StringBuilder();
			StringWriter outputWriter = new StringWriter(outputString);

			int exitCode = Run(command, outputWriter, outputWriter);

			outputWriter.Close();

			output = outputString.ToString();

			return exitCode;
		}

		/// <summary>
		/// Run the specified command through the shell specified in the COMSPEC 
		/// environment variable.
		/// </summary>
		/// <param name="command">The command to pass to the shell.</param>
		/// <param name="output">Results from standard output stream.</param>
		/// <param name="error">Results from standard error stream.</param>
		/// <returns>The exit code of the process.</returns>
		public static int Run(string command, out string output, out string error)
		{
			StringBuilder outputString = new StringBuilder();
			StringWriter outputWriter = new StringWriter(outputString);

			StringBuilder errorString = new StringBuilder();
			StringWriter errorWriter = new StringWriter(errorString);

			int exitCode = Run(command, outputWriter, errorWriter);

			outputWriter.Close();
			errorWriter.Close();

			output = outputString.ToString();
			error = errorString.ToString();

			return exitCode;
		}

		/// <summary>
		/// Run the specified command through the shell specified in the COMSPEC environment variable.
		/// </summary>
		/// <param name="command">The command to pass to the shell.</param>
		/// <param name="outputWriter">The stream to which the standard output stream will be redirected.</param>
		/// <param name="errorWriter">The stream to which the standard error stream will be redirected.</param>
		/// <returns>The exit code of the process.</returns>
		public static int Run(string command, TextWriter outputWriter, TextWriter errorWriter)
		{
			int exitCode = -1;
			string batchFileName = null;

			try
			{
				batchFileName = CreateBatchFile(command);

				if (debugMode)
				{
					outputWriter.WriteLine(command);
					return 0;
				}
				
				string newCmdString = "/c \"" + batchFileName + "\"";
				string comspec = System.Environment.GetEnvironmentVariable("COMSPEC");
				Process p = new Process();

				p.StartInfo = new ProcessStartInfo(comspec, newCmdString);
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.CreateNoWindow = true;
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.RedirectStandardError = true;

				if (outputWriter != null && errorWriter != null)
				{
					p.OutputDataReceived += delegate(object sender, DataReceivedEventArgs arg)
					{
						if (arg.Data != null)
						{
							outputWriter.WriteLine(arg.Data);
						}
					};

					p.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs arg)
					{
						if (arg.Data != null)
						{
							errorWriter.WriteLine(arg.Data);
						}
					};
				}

				p.Start();

				if (outputWriter != null && errorWriter != null)
				{
					p.BeginOutputReadLine();
					p.BeginErrorReadLine();
				}

				p.WaitForExit();

				exitCode = p.ExitCode;

				p.Close();
			}
			finally
			{
				if (batchFileName != null)
					File.Delete(batchFileName);
			}

			return exitCode;
		}

		static bool debugMode = false;

		/// <summary>
		/// Get or set debug execution mode.  In debug mode, just print the command that is supposed to be
		/// executed and return 0.
		/// </summary>
		public static bool DebugMode
		{
			get
			{
				return debugMode;
			}
			set
			{
				debugMode = value;
			}
		}
		
		#endregion
	}
}
