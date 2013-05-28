using System;
using System.Collections.Generic;
using System.Text;

namespace ToolBelt
{
    /// <summary>
    /// Interface for classes that process the command line
    /// </summary>
    public interface IProcessCommandLine
    {
        /// <summary>
        /// Process the command line arguments
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        void ProcessCommandLine(string[] args);
    }
}
