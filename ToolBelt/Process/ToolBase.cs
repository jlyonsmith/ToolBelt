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

            if (Type.GetType("Mono.Runtime") != null)
            {
                parser.CommandName = "mono " + Path.GetFileName(Assembly.GetEntryAssembly().Location);
            }

            parser.ParseAndSetTarget(args);
        }

        public virtual void WriteInfo(string format, params object[] args)
        {
            ConsoleUtility.WriteMessage(MessageType.Info, format, args);
        }

        public virtual void WriteMessage(string format, params object[] args)
        {
            ConsoleUtility.WriteMessage(MessageType.Normal, format, args);
        }

        public virtual void WriteWarning(string format, params object[] args)
        {
            ConsoleUtility.WriteMessage(MessageType.Warning, format, args);
        }

        public virtual void WriteError(string format, params object[] args)
        {
            ConsoleUtility.WriteMessage(MessageType.Error, format, args);
            HasOutputErrors = true;
        }
    }
}

