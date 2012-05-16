using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ToolBelt
{
    public sealed class ConsoleOutputter : IOutputter
    {
        #region Private Fields
        private TextWriter writer;

        #endregion

        #region Constructors
        public ConsoleOutputter()
        {
            writer = Console.Out;
        }

        public ConsoleOutputter(TextWriter writer)
        {
            this.writer = writer;
        }

        #endregion

        #region Public Properties
        public bool Silent { get; set; }

        #endregion

        #region IOutputter Implementation
        void IOutputter.OutputCustomEvent(OutputCustomEventArgs e)
        {
        }

        public void OutputErrorEvent(OutputErrorEventArgs e)
        {
            if (Silent)
                return;

            // In case there is a problem loading the resources
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;

                if (!String.IsNullOrEmpty(e.File))
                {
                    writer.Write("{0}({1}): ", e.File, e.LineNumber);
                }

                writer.Write(OutputterResources.Error, e.Code);
                writer.WriteLine(e.Message);
            }
            finally
            {
                Console.ResetColor();
            }
        }

        public void OutputWarningEvent(OutputWarningEventArgs e)
        {
            if (Silent)
                return;

            // In case of resource problems
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;

                if (!String.IsNullOrEmpty(e.File))
                {
                    writer.Write("{0}({1}): ", e.File, e.LineNumber);
                }

                writer.Write(OutputterResources.Warning, e.Code);
                writer.WriteLine(e.Message);
            }
            finally
            {
                Console.ResetColor();
            }
        }

        public void OutputMessageEvent(OutputMessageEventArgs e)
        {
            if (Silent)
                return;

            switch (e.Importance)
            {
                case MessageImportance.Low:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;

                case MessageImportance.High:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }

            writer.WriteLine(e.Message);
            Console.ResetColor();
        }
        #endregion
    }
}
