using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Collections.Specialized;

namespace ToolBelt
{
    /// <summary>
    /// Interface for tasks that process the configuration file
    /// </summary>
    public interface IProcessAppSettings
    {
        /// <summary>
        /// Process the appSettings section of the app.config file.
        /// </summary>
        void ProcessAppSettings(NameValueCollection collection);
    }
}
