using System;
using System.Collections.Generic;
using System.Text;

namespace ToolBelt
{
    public interface ITool
    {
        void Execute();
        bool HasOutputErrors { get; }
    }
}
