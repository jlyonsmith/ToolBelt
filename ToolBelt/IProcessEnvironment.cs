using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ToolBelt
{
    /// <summary>
    /// Interface for Task classes that process environment variables
    /// </summary>
    public interface IProcessEnvironment
    {
        /// <summary>
        /// Process the environment.
        /// </summary>
        /// <returns></returns>
        void ProcessEnvironment();
    }
}
