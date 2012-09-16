//
// This file genenerated by the Buckle tool on 9/16/2012 at 12:51 PM. 
//
// Contains strongly typed wrappers for resources in OutputterResources.resx
//

namespace ToolBelt {
using System;
using System.Reflection;
using System.Resources;
using System.Diagnostics;
using System.Globalization;


/// <summary>
/// Strongly typed resource wrappers generated from OutputterResources.resx.
/// </summary>
public class OutputterResources
{
    internal static readonly ResourceManager ResourceManager = new ResourceManager(typeof(OutputterResources));

    /// <summary>
    /// error: 
    /// </summary>
    public static ToolBelt.Message Error
    {
        get
        {
            return new ToolBelt.Message("Error", typeof(OutputterResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// warning: 
    /// </summary>
    public static ToolBelt.Message Warning
    {
        get
        {
            return new ToolBelt.Message("Warning", typeof(OutputterResources), ResourceManager, null);
        }
    }
}
}
