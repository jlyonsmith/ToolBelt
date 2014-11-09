using System;

namespace ToolBelt.NUnit
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class DeploymentItemAttribute : Attribute
    {
        #region Instance Constructors
        public DeploymentItemAttribute(string fromFilePath)
        {
            this.FromFilePath = fromFilePath;
        }

        #endregion

        #region Instance Properties
        public string FromFilePath { get; private set; }
        public string ToFilePath { get; set; }
        #endregion
    }
}

