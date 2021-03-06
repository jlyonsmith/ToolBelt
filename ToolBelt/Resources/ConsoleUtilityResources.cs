//
// This file genenerated by the Buckle tool on 11/9/2014 at 8:18 AM. 
//
// Contains strongly typed wrappers for resources in ConsoleUtilityResources.resx
//

namespace ToolBelt {
using System;
using System.Reflection;
using System.Resources;
using System.Diagnostics;
using System.Globalization;


/// <summary>
/// Strongly typed resource wrappers generated from ConsoleUtilityResources.resx.
/// </summary>
public class ConsoleUtilityResources
{
    internal static readonly ResourceManager ResourceManager = new ResourceManager(typeof(ConsoleUtilityResources));

    /// <summary>
    /// debug: 
    /// </summary>
    public static ToolBelt.Message Debug
    {
        get
        {
            return new ToolBelt.Message("Debug", typeof(ConsoleUtilityResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// info: 
    /// </summary>
    public static ToolBelt.Message Info
    {
        get
        {
            return new ToolBelt.Message("Info", typeof(ConsoleUtilityResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// error: 
    /// </summary>
    public static ToolBelt.Message Error
    {
        get
        {
            return new ToolBelt.Message("Error", typeof(ConsoleUtilityResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// warning: 
    /// </summary>
    public static ToolBelt.Message Warning
    {
        get
        {
            return new ToolBelt.Message("Warning", typeof(ConsoleUtilityResources), ResourceManager, null);
        }
    }
}
}
