using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Resources;

namespace Hammer
{
    public class HammerTool
    {
        public enum LineEnding
        {
            Auto,
            Cr,
            Lf,
            CrLf,
        }

        #region Fields
        public string InputFileName;
        public string OutputFileName;
        public LineEnding? FixedEndings;
        public bool NoLogo;
        public bool ShowUsage;

        public bool HasOutputErrors { get; set; }
        
        #endregion
        
        #region Constructors
        public HammerTool()
        {
        }
        
        #endregion      
        
        #region Methods
        public void Execute()
        {
            if (!NoLogo)
            {
                string version = ((AssemblyFileVersionAttribute)Assembly.GetExecutingAssembly()
                                  .GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true) [0]).Version;
                
                WriteMessage("Hammer text line ending fixer. Version {0}", version);
                WriteMessage("Copyright (c) 2012, John Lyon-Smith." + Environment.NewLine);
            }
            
            if (ShowUsage)
            {
                WriteMessage(@"Reports on and fixes line endings for text files.
    
Usage: mono Hammer.exe ...

Arguments:
    <text-file>              Input text file.
    [-o:<output-file>]       Specify different name for output file.
    [-f:<line-endings>]      Fix line endings to be cr, lf, crlf or auto.
    [-q]                     Suppress logo.
    [-h] or [-?]             Show help.
"
                );
                return;
            }

            if (InputFileName == null)
            {
                WriteError("A text file must be specified");
                return;
            }

            if (OutputFileName == null)
            {
                OutputFileName = InputFileName;
            }

            // Read the entire file and determine all the different line endings
            string fileContents = File.ReadAllText(InputFileName);

            int numCr = 0;
            int numLf = 0;
            int numCrLf = 0;
            int numLines = 1;

            for (int i = 0; i < fileContents.Length; i++)
            {
                char c = fileContents [i];
                char c1 = (i < fileContents.Length - 1 ? fileContents [i + 1] : '\0');

                if (c == '\r')
                {
                    if (c1 == '\n')
                    {
                        numCrLf++;
                        i++;
                        numLines++;
                    }
                    else
                    {
                        numCr++;
                        numLines++;
                    }
                }
                else if (c == '\n')
                {
                    numLf++;
                    numLines++;
                }
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("\"{0}\", lines={1}, cr={2}, lf={3}, crlf={4}", this.InputFileName, numLines, numCr, numLf, numCrLf);

            if (!FixedEndings.HasValue)
            {
                WriteMessage(sb.ToString());
                return;
            }
            
            LineEnding autoLineEnding = LineEnding.Auto;
            int n = 0;

            if (numLf > n)
            {
                autoLineEnding = LineEnding.Lf;
                n = numLf;
            }
            if (numCrLf > n)
            {
                autoLineEnding = LineEnding.CrLf;
                n = numCrLf;
            }
            if (numCr > n)
            {
                autoLineEnding = LineEnding.Cr;
            }

            if (this.FixedEndings == LineEnding.Auto)
                this.FixedEndings = autoLineEnding;

            string newLineChars = 
                this.FixedEndings == LineEnding.Cr ? "\r" :
                this.FixedEndings == LineEnding.Lf ? "\n" :
                "\r\n";

            n = 0;

            using (StreamWriter writer = new StreamWriter(OutputFileName))
            {
                for (int i = 0; i < fileContents.Length; i++)
                {
                    char c = fileContents [i];
                    char c1 = (i < fileContents.Length - 1 ? fileContents [i + 1] : '\0');

                    if (c == '\r')
                    {
                        if (c1 == '\n')
                        {
                            i++;
                        }

                        n++;
                        writer.Write(newLineChars);
                    }
                    else if (c == '\n')
                    {
                        n++;
                        writer.Write(newLineChars);
                    }
                    else
                    {
                        writer.Write(c);
                    }
                }
            }

            sb.AppendFormat(
                " -> \"{0}\", lines={1}, {2}={3}", 
                OutputFileName, 
                n + 1,
                FixedEndings.Value == LineEnding.Cr ? "cr" : 
                FixedEndings.Value == LineEnding.Lf ? "lf" : "crlf",
                n);

            WriteMessage(sb.ToString());
        }

        public void ProcessCommandLine(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    switch (arg [1])
                    {
                        case 'h':
                        case '?':
                            ShowUsage = true;
                            return;
                        case 'q':
                            NoLogo = true;
                            continue;
                        case 'o':
                            CheckAndSetArgument(arg, ref OutputFileName); 
                            continue;
                        case 'f':
                            string lineEndings = (FixedEndings.HasValue ? FixedEndings.Value.ToString() : null);
                            CheckAndSetArgument(arg, ref lineEndings); 
                            FixedEndings = (LineEnding)Enum.Parse(typeof(LineEnding), lineEndings, true);
                            break;
                        default:
                            throw new ApplicationException(string.Format("Unknown argument '{0}'", arg [1]));
                    }
                }
                else if (String.IsNullOrEmpty(InputFileName))
                {
                    InputFileName = arg;
                }
                else
                {
                    throw new ApplicationException("Only one file can be specified");
                }
            }
        }
        
        private void CheckAndSetArgument(string arg, ref string val)
        {
            if (arg [2] != ':')
            {
                throw new ApplicationException(string.Format("Argument {0} is missing a colon", arg [1]));
            }
            
            if (string.IsNullOrEmpty(val))
            {
                val = arg.Substring(3);
            }
            else
            {
                throw new ApplicationException(string.Format("Argument {0} has already been set", arg [1]));
            }
        }
        
        private void WriteError(string format, params object[] args)
        {
            Console.Write("error: ");
            Console.WriteLine(format, args);
            this.HasOutputErrors = true;
        }
        
        private void WriteWarning(string format, params object[] args)
        {
            Console.Write("warning: ");
            Console.WriteLine(format, args);
            this.HasOutputErrors = true;
        }
        
        private void WriteMessage(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
        
        #endregion
    }
}
