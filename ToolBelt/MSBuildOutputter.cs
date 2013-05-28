using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;

namespace ToolBelt
{
    public class MSBuildOutputter : IOutputter
    {
        #region Fields
        private IBuildEngine buildEngine;
        private string taskName;
        
        #endregion

        #region Construction
        public MSBuildOutputter(IBuildEngine buildEngine, string taskName)
        {
            this.buildEngine = buildEngine;
            this.taskName = taskName;
        }

        #endregion

        #region IOutputter Members

        public void OutputCustomEvent(OutputCustomEventArgs args)
        {
        }

        public void OutputErrorEvent(OutputErrorEventArgs args)
        {
            buildEngine.LogErrorEvent(new BuildErrorEventArgs(
                args.SubCategory, args.Code, args.File, 
                args.LineNumber, args.ColumnNumber, args.EndLineNumber, args.EndColumnNumber, 
                args.Message, args.HelpKeyword, taskName, args.Timestamp, null));
        }

        public void OutputWarningEvent(OutputWarningEventArgs args)
        {
            buildEngine.LogWarningEvent(new BuildWarningEventArgs(
                args.SubCategory, args.Code, args.File,
                args.LineNumber, args.ColumnNumber, args.EndLineNumber, args.EndColumnNumber,
                args.Message, "", args.SenderName, args.Timestamp, null));
        }

        public void OutputMessageEvent(OutputMessageEventArgs args)
        {
            buildEngine.LogMessageEvent(new BuildMessageEventArgs(
                args.Message, args.HelpKeyword, taskName, (Microsoft.Build.Framework.MessageImportance)(int)args.Importance));
        }

        #endregion
    }
}
