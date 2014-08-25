using System;
using ToolBelt;
using System.Reflection;
using System.IO;

namespace ToolBelt
{
    public abstract class ToolBase : ITool, IProcessCommandLine
    {
        CommandLineParser parser; 

        public virtual bool HasOutputErrors { get; protected set; }

        public virtual void Execute()
        {
        }

        protected CommandLineParser Parser
        {
            get
            {
                if (parser == null)
                    parser = new CommandLineParser(this);

                return parser;
            }
        }

        public void ProcessCommandLine(string[] args)
        {
            parser = new CommandLineParser(this);
            parser.CommandName = parser.ProcessName + " " + Path.GetFileName(Assembly.GetEntryAssembly().Location);
            parser.ParseAndSetTarget(args);
        }

        protected virtual void WriteInfo(string format, params object[] args)
        {
            ConsoleUtility.WriteMessage(MessageType.Normal, format, args);
        }

        protected virtual void WriteMessage(string format, params object[] args)
        {
            ConsoleUtility.WriteMessage(MessageType.Normal, format, args);
        }

        protected virtual void WriteWarning(string format, params object[] args)
        {
            ConsoleUtility.WriteMessage(MessageType.Warning, format, args);
        }

        protected virtual void WriteError(string format, params object[] args)
        {
            ConsoleUtility.WriteMessage(MessageType.Error, format, args);
            HasOutputErrors = true;
        }
    }
}

