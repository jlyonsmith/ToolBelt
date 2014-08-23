//
// This file genenerated by the Buckle tool on 8/23/2014 at 2:15 PM. 
//
// Contains strongly typed wrappers for resources in AppSettingsParserResources.resx
//

namespace ToolBelt {
using System;
using System.Reflection;
using System.Resources;
using System.Diagnostics;
using System.Globalization;


/// <summary>
/// Strongly typed resource wrappers generated from AppSettingsParserResources.resx.
/// </summary>
public class AppSettingsParserResources
{
    internal static readonly ResourceManager ResourceManager = new ResourceManager(typeof(AppSettingsParserResources));

    /// <summary>
    /// Invalid value '{0}' for appSetting '{1}'
    /// </summary>
    public static ToolBelt.Message InvalidValueForAppSettingArgument(object param0, object param1)
    {
        Object[] o = { param0, param1 };
        return new ToolBelt.Message("InvalidValueForAppSettingArgument", typeof(AppSettingsParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Invalid Initializer class '{0}' for appSetting '{1}'
    /// </summary>
    public static ToolBelt.Message InvalidInitializerClassForAppSettingArgument(object param0, object param1)
    {
        Object[] o = { param0, param1 };
        return new ToolBelt.Message("InvalidInitializerClassForAppSettingArgument", typeof(AppSettingsParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Invalid value {0} for appSetting '{1}'. Valid values are: {2}
    /// </summary>
    public static ToolBelt.Message InvalidValueForAppSettingArgumentWithValid(object param0, object param1, object param2)
    {
        Object[] o = { param0, param1, param2 };
        return new ToolBelt.Message("InvalidValueForAppSettingArgumentWithValid", typeof(AppSettingsParserResources), ResourceManager, o);
    }

    /// <summary>
    /// No way found to initialize type '{0}' for appSetting '{1}' from a String
    /// </summary>
    public static ToolBelt.Message NoWayToInitializeTypeFromString(object param0, object param1)
    {
        Object[] o = { param0, param1 };
        return new ToolBelt.Message("NoWayToInitializeTypeFromString", typeof(AppSettingsParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Property '{0}' is not a strongly typed array
    /// </summary>
    public static ToolBelt.Message PropertyIsNotAStronglyTypedArray(object param0)
    {
        Object[] o = { param0 };
        return new ToolBelt.Message("PropertyIsNotAStronglyTypedArray", typeof(AppSettingsParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Problem invoking the Add method for argument '{0}'
    /// </summary>
    public static ToolBelt.Message ProblemInvokingAddMethod(object param0)
    {
        Object[] o = { param0 };
        return new ToolBelt.Message("ProblemInvokingAddMethod", typeof(AppSettingsParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Property '{0}' should be readable and writeable
    /// </summary>
    public static ToolBelt.Message PropertyShouldBeReadableAndWriteable(object param0)
    {
        Object[] o = { param0 };
        return new ToolBelt.Message("PropertyShouldBeReadableAndWriteable", typeof(AppSettingsParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Property {0} does not have a value for Description
    /// </summary>
    public static ToolBelt.Message PropertyDoesNotHaveAValueForDescription(object param0)
    {
        Object[] o = { param0 };
        return new ToolBelt.Message("PropertyDoesNotHaveAValueForDescription", typeof(AppSettingsParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Property '{0}' has no Add method with non-object parameter type.
    /// </summary>
    public static ToolBelt.Message PropertyHasNoAddMethodWithNonObjectParameter(object param0)
    {
        Object[] o = { param0 };
        return new ToolBelt.Message("PropertyHasNoAddMethodWithNonObjectParameter", typeof(AppSettingsParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Property '{0}' is an array which is not supported. Please change it to a mutable ICollection derived type.
    /// </summary>
    public static ToolBelt.Message ArraysAreNotSupported(object param0)
    {
        Object[] o = { param0 };
        return new ToolBelt.Message("ArraysAreNotSupported", typeof(AppSettingsParserResources), ResourceManager, o);
    }
}
}
