using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace ToolBelt
{
    /// <summary>
    /// Interface for tasks that process the configuration file
    /// </summary>
    public interface IProcessConfiguration
    {
        /// <summary>
        /// Process the configuration at the specified user level
        /// </summary>
        /// <param name="userLevel"></param>
        /// <returns></returns>
        void ProcessConfiguration();
    }
}
