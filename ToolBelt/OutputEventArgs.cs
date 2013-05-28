using System;
using System.Threading;

namespace ToolBelt
{
    [Serializable]
    public enum MessageImportance
    {
        High = 0,
        Normal = 1,
        Low = 2
    }

    [Serializable]
    public class OutputEventArgs
    {
        public OutputEventArgs()
        {
        }

        public OutputEventArgs(
            string helpKeyword,
            string senderName,
            DateTime timestamp,
            int threadId,
            string message)
        {
            this.HelpKeyword = helpKeyword;
            this.SenderName = senderName;
            this.Timestamp = timestamp;
            this.ThreadId = threadId;
            this.Message = message;
        }

        public string HelpKeyword { get; private set; }
        public string SenderName { get; private set; }
        public DateTime Timestamp { get; private set; }
        public int ThreadId { get; set; }
        public string Message { get; private set; }
    }

    [Serializable]
    public class OutputPositionalEventArgs : OutputEventArgs
    {
        public OutputPositionalEventArgs()
        {
        }

        public OutputPositionalEventArgs(string message)
            : base("", "", DateTime.Now, 0, message)
        {
        }

        public OutputPositionalEventArgs(
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
            string message)
            : base(helpKeyword, senderName, timestamp, threadId, message)
        {
            this.SubCategory = subcategory;
            this.Code = code;
            this.File = file;
            this.LineNumber = lineNumber;
            this.ColumnNumber = columnNumber;
            this.EndLineNumber = endLineNumber;
            this.EndColumnNumber = endColumnNumber;
        }

        public string SubCategory { get; private set; }
        public string Code { get; private set; }
        public string File { get; private set; }
        public int LineNumber { get; private set; }
        public int ColumnNumber { get; private set; }
        public int EndLineNumber { get; private set; }
        public int EndColumnNumber { get; private set; }
    }

    [Serializable]
    public class OutputWarningEventArgs : OutputPositionalEventArgs
    {
        public OutputWarningEventArgs()
        {
        }

        public OutputWarningEventArgs(string message)
            : this("", "", "", 0, 0, 0, 0, "", "", DateTime.Now, 0, message)
        {
        }

        public OutputWarningEventArgs(
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
            string message)
            : base(subcategory, code, file, lineNumber, columnNumber, endLineNumber,
                endColumnNumber, helpKeyword, senderName, timestamp, threadId, message)
        {
        }
    }

    [Serializable]
    public class OutputErrorEventArgs : OutputPositionalEventArgs
    {
        public OutputErrorEventArgs()
        {
        }

        public OutputErrorEventArgs(string message)
            : this("", "", "", 0, 0, 0, 0, "", "", DateTime.Now, 0, message)
        {
        }

        public OutputErrorEventArgs(
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
            string message)
            : base(subcategory, code, file, lineNumber, columnNumber, endLineNumber,
                endColumnNumber, helpKeyword, senderName, timestamp, threadId, message)
        {
        }
    }

    [Serializable]
    public class OutputMessageEventArgs : OutputEventArgs
    {
        public OutputMessageEventArgs() { }
        public OutputMessageEventArgs(MessageImportance importance, string message)
            : base("", "", DateTime.Now, 0, message)
        {
            this.Importance = importance;
        }

        public MessageImportance Importance { get; private set; }
    }

    [Serializable]
    public class OutputCustomEventArgs : EventArgs
    {
        public OutputCustomEventArgs() : base() { }
    }
}

