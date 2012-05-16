using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace ToolBelt
{
    public sealed class TraceOutputter : IOutputter
    {
        #region Private Fields
        private List<string> errors = new List<string>();

        #endregion

        #region IOutputter Members
        
        void IOutputter.OutputCustomEvent(OutputCustomEventArgs e)
        {
        }

        void IOutputter.OutputErrorEvent(OutputErrorEventArgs e)
        {
            lock (errors)
            {
                errors.Add(Format(e.Code, e.Message));
            }
            Trace.WriteLine(Format(e.Code, e.Message));
        }

        void IOutputter.OutputMessageEvent(OutputMessageEventArgs e)
        {
            Trace.WriteLine(e.Message);
        }

        void IOutputter.OutputWarningEvent(OutputWarningEventArgs e)
        {
            Trace.WriteLine(Format(e.Code, e.Message));
        }

        #endregion

        #region Public Methods
        public IList<string> GetErrors()
        {
            lock (errors)
            {
                return new List<string>(errors).AsReadOnly();
            }
        }

        public string LastError
        {
            get
            {
                lock (errors)
                {
                    return errors.Count > 0 ? errors[errors.Count - 1] : string.Empty;
                }
            }
        }

        #endregion

        #region Private Methods
        private string Format(string code, string message)
        {
            if (string.IsNullOrEmpty(code))
                return message;
            return code + ": " + message;
        }

        #endregion
    }
}
