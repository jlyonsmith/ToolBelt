using System;
using System.Collections.Generic;
using System.Text;

namespace ToolBelt
{
    public sealed class NullOutputter : IOutputter
    {
        #region Constructors
        public NullOutputter()
        {
        }

        #endregion

        #region IOutputter Implementation
        void IOutputter.OutputCustomEvent(OutputCustomEventArgs e)
        {
        }

        public void OutputErrorEvent(OutputErrorEventArgs e)
        {
        }

        public void OutputWarningEvent(OutputWarningEventArgs e)
        {
        }

        public void OutputMessageEvent(OutputMessageEventArgs e)
        {
        }

        #endregion
    }
}
