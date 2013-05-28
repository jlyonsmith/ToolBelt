using System;
using System.Resources;
using System.Text.RegularExpressions;

namespace ToolBelt
{
    public class OutputHelper
    {
        #region Private Fields
        private IOutputter outputter;
        private bool hasOutputErrors;
        private bool warningsAsErrors;

        #endregion

        #region Constructors
        public OutputHelper(IOutputter outputter)
        {
            this.outputter = outputter;
            this.hasOutputErrors = false;
        }

        #endregion

        #region Public Properties
        public IOutputter Outputter 
        {
            get { return this.outputter; }
            set { this.outputter = value; } 
        }

        public bool WarningsAsErrors
        {
            get { return warningsAsErrors; }
            set { warningsAsErrors = value; }
        }

        #endregion
        
        #region Public Methods
        public void Message()
        {
            Message(MessageImportance.Normal, String.Empty);
        }

        public void Message(string message, params object[] messageArgs)
        {
            Message(MessageImportance.Normal, message, messageArgs);
        }

        public void Message(MessageImportance importance, string message, params object[] messageArgs)
        {
            this.outputter.OutputMessageEvent(
                new OutputMessageEventArgs(importance, messageArgs.Length == 0 ? message : message.CultureFormat(messageArgs)));
        }

        public void Error(string message, params object[] messageArgs)
        {
            Error("", 0, 0, message, messageArgs);
        }

        public void Error(
            string file,
            int lineNumber,
            int columnNumber,
            string message,
            params object[] messageArgs)
        {
            Error("", "", file, lineNumber, columnNumber, 0, 0, "", "", DateTime.Now, 0, message, messageArgs);
        }

        public void Error(
            string subcategory,
            string code,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string helpKeyword,
            string senderName,
            DateTime timestamp,
            int threadId, 
            string message, 
            params object[] messageArgs)
        {
            OutputErrorEventArgs e = new OutputErrorEventArgs(
                subcategory, 
                code,
                file,
                lineNumber, 
                columnNumber,
                endLineNumber,
                endColumnNumber,
                helpKeyword,
                senderName,
                timestamp,
                threadId,
                messageArgs.Length == 0 ? message : message.CultureFormat(messageArgs));

            this.hasOutputErrors = true;

            this.outputter.OutputErrorEvent(e);
        }

        public void Warning(string message, params object[] messageArgs)
        {
            Warning("", 0, 0, message, messageArgs);
        }

        public void Warning(
            string file,
            int lineNumber,
            int columnNumber,
            string message,
            params object[] messageArgs)
        {
            Warning("", "", file, lineNumber, columnNumber, 0, 0, "", "", DateTime.Now, 0, message, messageArgs);
        }

        public void Warning(
            string subcategory,
            string code,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string helpKeyword,
            string senderName,
            DateTime timestamp,
            int threadId,
            string message,
            params object[] messageArgs)
        {
            if (warningsAsErrors)
            {
                Error(
                    subcategory,
                    code,
                    file,
                    lineNumber,
                    columnNumber,
                    endLineNumber,
                    endColumnNumber,
                    helpKeyword,
                    senderName,
                    timestamp,
                    threadId,
                    message,
                    messageArgs);
                return;
            }

            OutputWarningEventArgs e = new OutputWarningEventArgs(
                subcategory,
                code,
                file,
                lineNumber,
                columnNumber,
                endLineNumber,
                endColumnNumber,
                helpKeyword,
                senderName,
                timestamp,
                threadId,
                messageArgs.Length == 0 ? message : message.CultureFormat(messageArgs));

            this.outputter.OutputWarningEvent(e);
        }

        public bool HasOutputErrors { get { return this.hasOutputErrors; } }

        #endregion    
    }
}
