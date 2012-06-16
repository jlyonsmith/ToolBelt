//
// This file genenerated by the Buckle tool on 6/15/2012 at 9:56 AM. 
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
    public static Message Debug
    {
        get
        {
            return new Message("Debug", typeof(ConsoleUtilityResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// error: 
    /// </summary>
    public static Message Error
    {
        get
        {
            return new Message("Error", typeof(ConsoleUtilityResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// warning: 
    /// </summary>
    public static Message Warning
    {
        get
        {
            return new Message("Warning", typeof(ConsoleUtilityResources), ResourceManager, null);
        }
    }
}
}
