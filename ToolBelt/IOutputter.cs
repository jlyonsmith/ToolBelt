using System;
using System.Collections.Generic;
using System.Text;

namespace ToolBelt
{
    public interface IOutputter
    {
        void OutputCustomEvent(OutputCustomEventArgs e);
        void OutputErrorEvent(OutputErrorEventArgs e);
        void OutputWarningEvent(OutputWarningEventArgs e);
        void OutputMessageEvent(OutputMessageEventArgs e);
    }
}
