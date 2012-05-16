using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using ToolBelt;

namespace Toaster
{
    public class Buckle : ITask
    {
        IBuildEngine buildEngine;
        ITaskHost taskHost;
        readonly string taskName = "Buckle";

        [Required]
        public string ResXFileName { get; set; }

        [Required]
        public string OutputFileName { get; set; }

        [Required]
        public string Namespace { get; set; }

        [Required]
        public string WrapperClass { get; set; }

        #region ITask Members

        public bool Execute()
        {
            BuckleTool tool = new BuckleTool(new MSBuildOutputter(buildEngine, taskName));

            tool.Parser.CommandName = taskName;
            tool.ResXFileName = new ParsedPath(this.ResXFileName, PathType.File);
            tool.OutputFileName = new ParsedPath(this.OutputFileName, PathType.File);
            tool.Namespace = this.Namespace;
            tool.WrapperClass = this.WrapperClass;
            tool.NoLogo = true;

            try
            {
                tool.Execute();
            }
            catch (Exception e)
            {
                tool.Output.Error(e.Message);
            }

            return !tool.Output.HasOutputErrors;
        }

        public IBuildEngine BuildEngine
        {
            get
            {
                return buildEngine;
            }
            set
            {
                buildEngine = value;
            }
        }

        public ITaskHost HostObject
        {
            get
            {
                return taskHost;
            }
            set
            {
                this.taskHost = value;
            }
        }

        #endregion
    }
}
