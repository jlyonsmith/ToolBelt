using System;
using ToolBelt;
using System.Reflection;
using System.IO;
using System.Configuration;
using System.Collections.Specialized;

namespace ToolBelt
{
    public abstract class ToolBase : ITool, IProcessCommandLine
    {
        CommandLineParser commandLineParser;
        AppSettingsParser appSettingsParser;

        public virtual bool HasOutputErrors { get; protected set; }

        public virtual void Execute()
        {
        }

        protected CommandLineParser Parser
        {
            get
            {
                if (commandLineParser == null)
                    commandLineParser = new CommandLineParser(this);

                return commandLineParser;
            }
        }

        protected AppSettingsParser SettingsParser
        {
            get
            {
                if (appSettingsParser == null)
                    appSettingsParser = new AppSettingsParser(this);

                return appSettingsParser;
            }
        }

        public void ProcessAppSettings(NameValueCollection collection = null)
        {
            if (collection == null)
                collection = ConfigurationManager.AppSettings;

            SettingsParser.ParseAndSetTarget(collection);
        }

        public void ProcessCommandLine(string[] args)
        {
            Parser.ParseAndSetTarget(args);

            if (Type.GetType("Mono.Runtime") != null)
            {
                var entryAssembly = Assembly.GetEntryAssembly();

                if (entryAssembly != null)
                    Parser.CommandName = "mono " + Path.GetFileName(entryAssembly.Location);
            }
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

