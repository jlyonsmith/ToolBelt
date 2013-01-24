//
// This file genenerated by the Buckle tool on 6/15/2012 at 9:56 AM. 
//
// Contains strongly typed wrappers for resources in CommandLineParserResources.resx
//

namespace ToolBelt {
using System;
using System.Reflection;
using System.Resources;
using System.Diagnostics;
using System.Globalization;


/// <summary>
/// Strongly typed resource wrappers generated from CommandLineParserResources.resx.
/// </summary>
public class CommandLineParserResources
{
    internal static readonly ResourceManager ResourceManager = new ResourceManager(typeof(CommandLineParserResources));

    /// <summary>
    /// command
    /// </summary>
    public static Message Command_Lowercase
    {
        get
        {
            return new Message("Command_Lowercase", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// Command argument already defined
    /// </summary>
    public static Message CommandArgumentAlreadyDefined
    {
        get
        {
            return new Message("CommandArgumentAlreadyDefined", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// CommandLineCopyrightAttribute or AssemblyCopyrightAttribute not found
    /// </summary>
    public static Message CopyrightAttributesNotFound
    {
        get
        {
            return new Message("CopyrightAttributesNotFound", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// Default argument already defined
    /// </summary>
    public static Message DefaultArgumentAlreadyDefined
    {
        get
        {
            return new Message("DefaultArgumentAlreadyDefined", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// CommandLineDescriptionAttribute or AssemblyDescriptionAttribute not found
    /// </summary>
    public static Message DescriptionAttributesNotFound
    {
        get
        {
            return new Message("DescriptionAttributesNotFound", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// Duplicate command line argument '{0}'
    /// </summary>
    public static Message DuplicateCommandLineArgument(object param0)
    {
        Object[] o = { param0 };
        return new Message("DuplicateCommandLineArgument", typeof(CommandLineParserResources), ResourceManager, o);
    }

    /// <summary>
    /// file-name
    /// </summary>
    public static Message FileName_Lowercase
    {
        get
        {
            return new Message("FileName_Lowercase", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// Invalid Initializer class '{0}' for command line argument '{1}'
    /// </summary>
    public static Message InvalidInitializerClassForCommandLineArgument(object param0, object param1)
    {
        Object[] o = { param0, param1 };
        return new Message("InvalidInitializerClassForCommandLineArgument", typeof(CommandLineParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Invalid value '{0}' for command line argument '{1}'.
    /// </summary>
    public static Message InvalidValueForCommandLineArgument(object param0, object param1)
    {
        Object[] o = { param0, param1 };
        return new Message("InvalidValueForCommandLineArgument", typeof(CommandLineParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Invalid value {0} for command line argument '{1}'. Valid values are: {2}
    /// </summary>
    public static Message InvalidValueForCommandLineArgumentWithValid(object param0, object param1, object param2)
    {
        Object[] o = { param0, param1, param2 };
        return new Message("InvalidValueForCommandLineArgumentWithValid", typeof(CommandLineParserResources), ResourceManager, o);
    }

    /// <summary>
    /// No way found to initialize type '{0}' for argument '{1}' from a string
    /// </summary>
    public static Message NoWayToInitializeTypeFromString(object param0, object param1)
    {
        Object[] o = { param0, param1 };
        return new Message("NoWayToInitializeTypeFromString", typeof(CommandLineParserResources), ResourceManager, o);
    }

    /// <summary>
    /// number
    /// </summary>
    public static Message Number_Lowercase
    {
        get
        {
            return new Message("Number_Lowercase", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// switches
    /// </summary>
    public static Message Switches_Lowercase
    {
        get
        {
            return new Message("Switches_Lowercase", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// Switches:
    /// </summary>
    public static Message Switches_Propercase
    {
        get
        {
            return new Message("Switches_Propercase", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// Problem invoking the Add method for argument '{0}'
    /// </summary>
    public static Message ProblemInvokingAddMethod(object param0)
    {
        Object[] o = { param0 };
        return new Message("ProblemInvokingAddMethod", typeof(CommandLineParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Property {0} does not have a value for Description
    /// </summary>
    public static Message PropertyDoesNotHaveAValueForDescription(object param0)
    {
        Object[] o = { param0 };
        return new Message("PropertyDoesNotHaveAValueForDescription", typeof(CommandLineParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Property '{0}' has no Add method with non-object parameter type.
    /// </summary>
    public static Message PropertyHasNoAddMethodWithNonObjectParameter(object param0)
    {
        Object[] o = { param0 };
        return new Message("PropertyHasNoAddMethodWithNonObjectParameter", typeof(CommandLineParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Property '{0}' is not a strongly typed array
    /// </summary>
    public static Message PropertyIsNotAStronglyTypedArray(object param0)
    {
        Object[] o = { param0 };
        return new Message("PropertyIsNotAStronglyTypedArray", typeof(CommandLineParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Property '{0}' should be readable and writeable
    /// </summary>
    public static Message PropertyShouldBeReadableAndWriteable(object param0)
    {
        Object[] o = { param0 };
        return new Message("PropertyShouldBeReadableAndWriteable", typeof(CommandLineParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Resource reader '{0}' does not contain a property '{1}'
    /// </summary>
    public static Message ResourceReaderDoesNotContainString(object param0, object param1)
    {
        Object[] o = { param0, param1 };
        return new Message("ResourceReaderDoesNotContainString", typeof(CommandLineParserResources), ResourceManager, o);
    }

    /// <summary>
    /// (Short format: /{0})
    /// </summary>
    public static Message ShortFormat(object param0)
    {
        Object[] o = { param0 };
        return new Message("ShortFormat", typeof(CommandLineParserResources), ResourceManager, o);
    }

    /// <summary>
    /// text
    /// </summary>
    public static Message Text_LowerCase
    {
        get
        {
            return new Message("Text_LowerCase", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// Type '{0}' is not derived from type '{1}'
    /// </summary>
    public static Message TypeNotDerivedFromType(object param0, object param1)
    {
        Object[] o = { param0, param1 };
        return new Message("TypeNotDerivedFromType", typeof(CommandLineParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Unknown or invalid argument '{0}'
    /// </summary>
    public static Message UnknownArgument(object param0)
    {
        Object[] o = { param0 };
        return new Message("UnknownArgument", typeof(CommandLineParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Unknown command '{0}'
    /// </summary>
    public static Message UnknownCommandArgument(object param0)
    {
        Object[] o = { param0 };
        return new Message("UnknownCommandArgument", typeof(CommandLineParserResources), ResourceManager, o);
    }

    /// <summary>
    /// Unprocessed argument already defined
    /// </summary>
    public static Message UnprocessedArgumentAlreadyDefined
    {
        get
        {
            return new Message("UnprocessedArgumentAlreadyDefined", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// Unprocessed argument must be array or collection
    /// </summary>
    public static Message UnprocessedArgumentMustBeArrayOrCollection
    {
        get
        {
            return new Message("UnprocessedArgumentMustBeArrayOrCollection", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// A target for unprocessed arguments cannot be specified without a default argument target
    /// </summary>
    public static Message UnprocessedRequiresDefaultArguments
    {
        get
        {
            return new Message("UnprocessedRequiresDefaultArguments", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// Syntax:
    /// </summary>
    public static Message Syntax_Propercase
    {
        get
        {
            return new Message("Syntax_Propercase", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// value
    /// </summary>
    public static Message Value_Lowercase
    {
        get
        {
            return new Message("Value_Lowercase", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// AssemblyFileVersionAttribute not found
    /// </summary>
    public static Message VersionAttributesNotFound
    {
        get
        {
            return new Message("VersionAttributesNotFound", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// Commands:
    /// </summary>
    public static Message Commands_Propercase
    {
        get
        {
            return new Message("Commands_Propercase", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// Description:
    /// </summary>
    public static Message Description_Propercase
    {
        get
        {
            return new Message("Description_Propercase", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// Examples:
    /// </summary>
    public static Message Examples_Propercase
    {
        get
        {
            return new Message("Examples_Propercase", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// Response files nested too deeply:
    /// </summary>
    public static Message ResponseFilesTooDeep
    {
        get
        {
            return new Message("ResponseFilesTooDeep", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }

    /// <summary>
    /// Could not open response file '{0}'
    /// </summary>
    public static Message ResponseFileUnopened(object param0)
    {
        Object[] o = { param0 };
        return new Message("ResponseFileUnopened", typeof(CommandLineParserResources), ResourceManager, o);
    }

    /// <summary>
    /// CommandLineTitleAttribute or AssemblyTitleAttribute not found
    /// </summary>
    public static Message TitleAttributesNotFound
    {
        get
        {
            return new Message("TitleAttributesNotFound", typeof(CommandLineParserResources), ResourceManager, null);
        }
    }
}
}
