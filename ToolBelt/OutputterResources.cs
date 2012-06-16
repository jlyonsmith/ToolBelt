//
// This file genenerated by the Buckle tool on 6/15/2012 at 9:56 AM. 
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
    public static Message Error
    {
        get
        {
            return new Message("Error", typeof(OutputterResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// warning: 
    /// </summary>
    public static Message Warning
    {
        get
        {
            return new Message("Warning", typeof(OutputterResources), ResourceManager, null);
        }
    }
}
}
