using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Threading;
using ToolBelt;

namespace ToolBelt
{
    /// <summary>
    /// Class for running shell commands and scripts
    /// </summary>
    public class Command
    {
        #region Fields
        /// <summary>
        /// Get or set debug execution mode.  In debug mode, just print the command that is supposed to be
        /// executed and return 0.
        /// </summary>
        public bool DebugMode { get; set; }
        public string Script { get; private set; }
        public TextWriter OutputWriter { get; private set; }
        public TextWriter ErrorWriter { get; private set; }

        #endregion

        #region Construction
        public Command(string script, TextWriter outputWriter, TextWriter errorWriter, bool debugMode)
        {
            this.Script = script;
            this.OutputWriter = outputWriter;
            this.ErrorWriter = errorWriter;
            this.DebugMode = debugMode;
        }
        #endregion

        #region Private Methods
        private string CreateScriptFile(string script)
        {
#if WINDOWS
            string scriptContents = String.Format("@echo off\r\n{0}\r\n", programAndArgs);
            ParsedPath scriptFileName = new ParsedPath(Path.GetTempFileName(), PathType.File).WithExtension(".bat");
#elif MACOS
            string scriptContents = String.Format("{0}\nexit $?", script);
            ParsedPath scriptFileName = new ParsedPath(Path.GetTempFileName(), PathType.File).WithExtension(".sh");
#else
#error Unsupported OS
#endif

            File.WriteAllText(scriptFileName, scriptContents);

            if (this.DebugMode)
            {
                Console.WriteLine(scriptFileName + ":");
                Console.WriteLine(scriptContents);
            }

            return scriptFileName;
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Run the specified command through the shell specified in the COMSPEC
        /// environment variable.  Output is sent to the default console output and error streams.
        /// </summary>
        /// <param name="programAndArgs">The command to pass to the shell.</param>
        /// <returns>The exit code of the process.</returns>
        public static int Run(string programAndArgs)
        {
            return Run(programAndArgs, Console.Out, Console.Error, false);  // Please DO NOT change this behavior!
        }

        /// <summary>
        /// Run the specified command through the shell specified in the COMSPEC 
        /// environment variable.
        /// </summary>
        /// <param name="programAndArgs">The command to pass to the shell.</param>
        /// <param name="output">Interleaved results from standard output and standard error streams.</param>
        /// <returns>The exit code of the process.</returns>
        public static int Run(string programAndArgs, out string output)
        {
            return Run(programAndArgs, out output, false);
        }

        public static int Run(string programAndArgs, out string output, bool debugMode)
        {
            StringBuilder outputString = new StringBuilder();
            StringWriter outputWriter = new StringWriter(outputString);

            int exitCode = Run(programAndArgs, outputWriter, outputWriter, debugMode);

            outputWriter.Close();

            output = outputString.ToString();

            return exitCode;
        }

        /// <summary>
        /// Run the specified command through the shell specified in the COMSPEC 
        /// environment variable.
        /// </summary>
        /// <param name="programAndArgs">The command to pass to the shell.</param>
        /// <param name="output">Results from standard output stream.</param>
        /// <param name="error">Results from standard error stream.</param>
        /// <returns>The exit code of the process.</returns>
        public static int Run(string programAndArgs, out string output, out string error)
        {
            StringBuilder outputString = new StringBuilder();
            StringWriter outputWriter = new StringWriter(outputString);

            StringBuilder errorString = new StringBuilder();
            StringWriter errorWriter = new StringWriter(errorString);

            int exitCode = Run(programAndArgs, outputWriter, errorWriter, false);

            outputWriter.Close();
            errorWriter.Close();

            output = outputString.ToString();
            error = errorString.ToString();

            return exitCode;
        }

        /// <summary>
        /// Run the specified command through the shell specified in the COMSPEC or SHELL environment variable.
        /// </summary>
        /// <param name="programAndArgs">The command to pass to the shell.</param>
        /// <param name="outputWriter">The stream to which the standard output stream will be redirected.</param>
        /// <param name="errorWriter">The stream to which the standard error stream will be redirected.</param>
        /// <param name="debugMode">Debug mode</param>
        /// <returns>The exit code of the process.</returns>
        public static int Run(string programAndArgs, TextWriter outputWriter, TextWriter errorWriter, bool debugMode)
        {
            return new Command(programAndArgs, outputWriter, errorWriter, debugMode).Run();
        }
            
        public int Run()
        {
            int exitCode = -1;
            string scriptFileName = null;

            try
            {
                scriptFileName = CreateScriptFile(this.Script);

                if (this.DebugMode)
                {
                    this.OutputWriter.WriteLine(this.Script);
                    return 0;
                }

#if WINDOWS
                var shellVar = "COMSPEC";
#elif MACOS || UNIX
                var shellVar = "SHELL";
#else 
                throw new NotImplementedException();
#endif
                string shell = System.Environment.GetEnvironmentVariable(shellVar);
                string argString = "\"" + scriptFileName + "\"";

                Process p = new Process();

                p.StartInfo = new ProcessStartInfo(shell, argString);
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.WorkingDirectory = Environment.CurrentDirectory;

                if (this.OutputWriter != null && this.ErrorWriter != null)
                {
                    p.OutputDataReceived += delegate(object sender, DataReceivedEventArgs arg)
                    {
                        if (arg.Data != null)
                        {
                            this.OutputWriter.WriteLine(arg.Data);
                        }
                    };

                    p.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs arg)
                    {
                        if (arg.Data != null)
                        {
                            this.ErrorWriter.WriteLine(arg.Data);
                        }
                    };
                }

                p.Start();

                if (this.OutputWriter != null && this.ErrorWriter != null)
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
                if (scriptFileName != null)
                    File.Delete(scriptFileName);
            }

            return exitCode;
        }

        #endregion
    }
}
