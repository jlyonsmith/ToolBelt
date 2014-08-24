using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Security.Permissions;
using System.Runtime.Serialization;
using System.Linq;

namespace ToolBelt
{
    /// <summary>
    /// A valid command line argument 
    /// </summary>
    public class CommandLineArgument
    {
        #region Fields

        private object argTarget;
        private Type argType;
        private PropertyInfo argPropertyInfo;
        private MethodInfo argAddMethod;
        private CommandLineArgumentAttribute argAttribute;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the underlying <see cref="Type" /> of the argument.
        /// </summary>
        /// <remarks>
        /// If the <see cref="Type" /> of the argument is a collection type,
        /// this property will returns the underlying type of the collection.
        /// </remarks>
        public Type ArgType
        {
            get { return argType; }
        }

        /// <summary>
        /// Gets the current argument value.
        /// </summary>
        /// <value>The argument value.</value>
        public object Value
        {
            get 
            {
                return argPropertyInfo.GetValue(argTarget,
                    BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets the current argument value <see cref="ICollection"/> if the argument allows multiple instances, otherwise <code>null</code>
        /// </summary>
        /// <value>The argument value collection.</value>
        public ICollection ValueCollection
        {
            get 
            {
                return Value as ICollection;
            }
        }

        /// <summary>
        /// Gets the name of the argument.
        /// </summary>
        /// <value>The long name of the argument.</value>
        public string Name
        {
            get { return argAttribute.Name; }
        }

        /// <summary>
        /// Gets the short name of the argument.
        /// </summary>
        public string ShortName
        {
            get { return argAttribute.ShortName; }
        }

        /// <summary>
        /// Gets the description of the argument.
        /// </summary>
        public string Description
        {
            get { return argAttribute.Description; }
        }

        /// <summary>
        /// Gets the hint for the argument value.
        /// </summary>
        public string ValueHint
        {
            get { return argAttribute.ValueHint; }
        }

        /// <summary>
        /// Gets a value indicating whether an instance of this argument is set.  A <see cref="ValueType"/> argument is always 
        /// considered not set.  
        /// </summary>
        public bool HasValue
        {
            get 
            { 
                if (AllowsMultiple)
                    return (ValueCollection != null && ValueCollection.Count > 0);
                else if (!ArgType.IsValueType || Nullable.GetUnderlyingType(ArgType) != null)
                    return Value != null;
                else
                    return true;
            }
        }

        /// <summary>
        /// Gets a count of the number of instances of this argument.
        /// </summary>
        public int Count
        {
            get
            {
                if (ValueCollection != null)
                    return ValueCollection.Count;
                else
                    return 1;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the argument can be specified multiple times.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the argument may be specified multiple 
        /// times; otherwise, <see langword="false" />.
        /// </value>
        public bool AllowsMultiple
        {
            get { return typeof(ICollection).IsAssignableFrom(argPropertyInfo.PropertyType); }
        }

        /// <summary>
        /// Gets a value indicating whether the argument is the default argument.
        /// </summary>
        public bool IsDefaultArg
        {
            get { return argAttribute is DefaultCommandLineArgumentAttribute; }
        }

        /// <summary>
        /// Gets a value indicating whether the argument is the unprocessed argument.
        /// </summary>
        public bool IsUnprocessedArg
        {
            get { return argAttribute is UnprocessedCommandLineArgumentAttribute; }
        }

        /// <summary>
        /// Gets a value indicating whether the argument is the command argument.
        /// </summary>
        public bool IsCommandArg
        {
            get { return argAttribute is CommandCommandLineArgumentAttribute; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new command line argument.
        /// </summary>
        /// <param name="attribute">The attribute that defines the argument</param>
        /// <param name="propertyInfo">The arguments property information</param>
        internal CommandLineArgument(object argTarget, CommandLineArgumentAttribute argAttribute, PropertyInfo argPropertyInfo)
        {
            // Set these two things first
            this.argTarget = argTarget;
            this.argType = argPropertyInfo.PropertyType;
            this.argPropertyInfo = argPropertyInfo;
            this.argAttribute = argAttribute;

            if (typeof(ICollection).IsAssignableFrom(argType))
            {
                if (argType.IsArray)
                    throw new NotSupportedException(CommandLineParserResources.ArraysAreNotSupported(argPropertyInfo.Name));

                // Locate Add method with 1 parameter that is not System.Object
                foreach (MethodInfo method in argPropertyInfo.PropertyType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (method.Name == "Add" && method.GetParameters().Length == 1)
                    {
                        ParameterInfo parameter = method.GetParameters()[0];

                        if (parameter.ParameterType != typeof(object))
                        {
                            argAddMethod = method;
                            argType = parameter.ParameterType;
                            break;
                        }
                    }
                }

                // If we didn't find an appropriate Add method we can't write to the collection
                if (argAddMethod == null)
                {
                    throw new NotSupportedException(CommandLineParserResources.PropertyHasNoAddMethodWithNonObjectParameter(argPropertyInfo.Name));
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Parse string value and assign to the target object property 
        /// </summary>
        /// <param name="value">The value that should be assigned to the argument.</param>
        /// <exception cref="CommandLineArgumentException">
        /// <para>Duplicate argument OR invalid value.</para>
        /// </exception>
        public void ParseAndSetTarget(string argString)
        {
            // Specifically not initialized so we can see if the big if block below has any holes
            object value = null;

            try
            {
                // Don't do any processing if this is to be an unprocessed argument list (which by definition must 
                // be an array or a collection)
                if (IsUnprocessedArg)
                {
                    value = argString;
                }
                else if ((argString == null && argType != typeof(bool)) || (argString != null && argString.Length == 0))
                {
                    // Null is only valid for bool variables.  Empty string is never valid
                    throw new CommandLineArgumentException(CommandLineParserResources.InvalidValueForCommandLineArgument(argString, Name));
                }
                else if (this.argAttribute.Initializer != null)
                {
                    // If there is an initializer type/method use it.
                    // Look for a public static Parse method on the initializer class
                    System.Reflection.MethodInfo parseMethod = argAttribute.Initializer.GetMethod(
                                                                   this.argAttribute.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null,
                                                                   CallingConventions.Standard, new Type[] { typeof(string) }, null);

                    if (parseMethod != null)
                    {
                        // Call the Parse method
                        value = parseMethod.Invoke(null, BindingFlags.Default, null,
                            new object[] { argString }, CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        throw new CommandLineArgumentException(
                            CommandLineParserResources.InvalidInitializerClassForCommandLineArgument(argAttribute.Initializer.ToString(), Name));
                    }
                }
                else if (argType == typeof(string))
                {
                    value = argString;
                }
                else if (argType == typeof(bool))
                {
                    value = (object)true;
                }
                else if (argType.IsEnum)
                {
                    try
                    {
                        value = Enum.Parse(argType, argString, true);
                    }
                    catch (ArgumentException ex)
                    {
                        string t = String.Empty;
                            
                        foreach (object obj in Enum.GetValues(ArgType))
                        {
                            t += obj.ToString() + ", ";
                        }
                            
                        // strip last ,
                        t = t.Substring(0, argString.Length - 2) + ".";
                            
                        throw new CommandLineArgumentException(
                            CommandLineParserResources.InvalidValueForCommandLineArgumentWithValid(t, Name, t), ex);
                    }
                }
                else if (argType.IsGenericType && argType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                {
                    // Check if this is a Nullable type, and get the underlying type if it is
                    Type underlyingType = Nullable.GetUnderlyingType(argType);

                    // Look for a public static Parse method on the type of the underlying property
                    System.Reflection.MethodInfo parseMethod = underlyingType.GetMethod(
                                                                   "Parse", BindingFlags.Public | BindingFlags.Static, null,
                                                                   CallingConventions.Standard, new Type[] { typeof(string) }, null);

                    if (parseMethod != null)
                    {
                        // Call the Parse method
                        value = parseMethod.Invoke(null, BindingFlags.Default, null,
                            new object[] { argString }, CultureInfo.InvariantCulture);
                    }
                }
                else
                {
                    // Look for a public static Parse method on the type of the property
                    System.Reflection.MethodInfo parseMethod = argType.GetMethod(
                                                                   "Parse", BindingFlags.Public | BindingFlags.Static, null,
                                                                   CallingConventions.Standard, new Type[] { typeof(string) }, null);

                    if (parseMethod != null)
                    {
                        // Call the Parse method
                        value = parseMethod.Invoke(null, BindingFlags.Default, null,
                            new object[] { argString }, CultureInfo.InvariantCulture);
                    }
                    else if (argType.IsClass)
                    {
                        // Search for a constructor that takes a string argument
                        ConstructorInfo stringArgumentConstructor =
                            ArgType.GetConstructor(new Type[] { typeof(string) });

                        if (stringArgumentConstructor != null)
                        {
                            value = stringArgumentConstructor.Invoke(BindingFlags.Default, null, new object[] { argString }, CultureInfo.InvariantCulture);
                        }
                    }
                }

                if (value == null)
                {
                    throw new CommandLineArgumentException(CommandLineParserResources.NoWayToInitializeTypeFromString(ArgType.Name, Name));
                }
            }
            catch (Exception e)
            {
                if (!(e is CommandLineArgumentException))
                {
                    throw new CommandLineArgumentException(CommandLineParserResources.InvalidValueForCommandLineArgument(argString, Name), e);
                }
                else
                    throw;
            }
                
            try
            {
                if (AllowsMultiple)
                {
                    var collection = ValueCollection;

                    // If value of property is null, create new instance of collection 
                    if (collection == null)
                    {
                        collection = (ICollection)Activator.CreateInstance(argPropertyInfo.PropertyType,
                            BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.InvariantCulture);

                        argPropertyInfo.SetValue(argTarget, collection, BindingFlags.Default,
                            null, null, CultureInfo.InvariantCulture);
                    }

                    try
                    {
                        argAddMethod.Invoke(collection, BindingFlags.Default, null,
                            new object[] { value }, CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex)
                    {
                        // So that the Add method can throw parsing errors
                        if (ex.InnerException is CommandLineArgumentException)
                            throw ex.InnerException;
                        else
                            throw new NotSupportedException(CommandLineParserResources.ProblemInvokingAddMethod(Name), ex);
                    }
                }
                else
                {
                    argPropertyInfo.SetValue(argTarget, value,
                        BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
                }
            }
            catch (TargetInvocationException e)
            {
                // Rethrow this exception as the inner exception if it a command line arguement exception.  That 
                // is one way the user can check for correct command line arguments
                if (e.InnerException != null && e.InnerException is CommandLineArgumentException)
                    throw e.InnerException;

                // Otherwise just keep on throwing up
                throw;
            }
        }

        /// <summary>
        /// Tests if the given command is valid.  A null or empty command is always valid.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public bool IsValidForCommand(string command)
        {
            if (String.IsNullOrEmpty(command))
                return true;
            
            if (argAttribute.Commands == null)
                return false;

            string[] commands = argAttribute.Commands.Split(',', ';');
            
            for (int i = 0; i < commands.Length; i++)
                commands[i] = commands[i].Trim().ToLower(CultureInfo.InvariantCulture); // commands are not localized
                
            return Array.Exists<string>(commands, delegate(string item)
            {
                return String.CompareOrdinal(item, command) == 0;
            });
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Returns the value of the argument as a string
        /// </summary>
        public override string ToString()
        {
            if (!HasValue)
                return String.Empty;

            if (this.IsDefaultArg || this.IsUnprocessedArg || this.IsCommandArg)
            {
                // These args need to be left unformatted
                StringBuilder sb = new StringBuilder();

                if (AllowsMultiple)
                {
                    IEnumerator enumerator = ValueCollection.GetEnumerator();
                    bool more = enumerator.MoveNext();

                    while (more)
                    {
                        more = enumerator.MoveNext();
                        sb.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}", MaybeQuoteString(enumerator.Current.ToString()), more ? " " : "");
                    }
                }
                else
                {
                    sb.Append(MaybeQuoteString(Value.ToString()));
                }

                return sb.ToString();
            }
            else
            {
                StringBuilder sb = new StringBuilder();

                if (AllowsMultiple)
                {
                    IEnumerator enumerator = ValueCollection.GetEnumerator();
                    bool more = enumerator.MoveNext();

                    while (more)
                    {
                        var value = enumerator.Current;
                        more = enumerator.MoveNext();

                        if (ArgType == typeof(bool))
                        {
                            if ((bool)value)
                                sb.AppendFormat(CultureInfo.InvariantCulture, "-{0}{1}", Name, more ? " " : "");
                        }
                        else
                        {
                            sb.AppendFormat(CultureInfo.InvariantCulture, "-{0}:{1}{2}", Name, MaybeQuoteString(value.ToString()), more ? " " : "");
                        }
                    }
                }
                else
                {
                    if (ArgType == typeof(bool))
                    {
                        if ((bool)Value)
                            sb.AppendFormat(CultureInfo.InvariantCulture, "-{0}", Name);
                    }
                    else
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "-{0}:{1}", Name, MaybeQuoteString(Value.ToString()));
                    }
                }

                return sb.ToString();
            }
        }

        #endregion

        #region Private Methods
        string MaybeQuoteString(string value)
        {
            bool quote = (value.ToString().IndexOf(' ') != -1);

            return quote ? "\"" + value + "\"" : value;
        }
        #endregion
    }

    /// <summary>
    /// Command line parser.
    /// </summary>
    public class CommandLineParser
    {
        #region Fields

        private Type argTargetType;
        private List<CommandLineArgument> argCollection;
        private CommandLineArgument defaultArgument;
        private CommandLineArgument unprocessedArgument;
        private CommandLineArgument commandArgument;
        private Type resourceReaderType;
        private CommandLineParserFlags flags;
        private string copyright;
        private string configuration;
        private string version;
        private string title;
        private string description;
        private string commandName;
        private Stack<string> responseFiles;
        private const int maxResponseFileDepth = 10;
        private Dictionary<string, string[]> commandArguments;
        private Dictionary<string, bool> setStates;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a logo banner for the program.
        /// </summary>
        public virtual string LogoBanner
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                StringWriter wr = new StringWriter(sb, CultureInfo.CurrentCulture);

                wr.Write(this.Title);
                wr.Write(". ");
                if (this.Configuration != String.Empty)
                {
                    wr.Write(this.Configuration);
                    wr.Write(" ");
                }
                wr.Write("Version ");
                wr.Write(this.Version);
                wr.WriteLine();
                wr.Write(this.Copyright);
                if (!this.Copyright.EndsWith("."))
                    wr.Write(".");
                wr.WriteLine();
                wr.Flush();

                return sb.ToString();
            }
        }

        /// <summary>
        /// A name that will be used in the <see cref="Usage"/> property.  Defaults to <see cref="ProcessName"/> and can
        /// be overridden.
        /// </summary>
        public string CommandName
        {
            get
            {
                if (commandName == null)
                    commandName = ProcessName;

                return commandName;
            }
            set
            {
                commandName = value;
            }
        }

        /// <summary>
        /// Gets the name of the current process
        /// </summary>
        public string ProcessName
        {
            get
            {
                return Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName).ToLower(CultureInfo.CurrentCulture);
            }
        }

        /// <summary>
        /// Gets the usage instructions.  For command base command lines this is the list of commands and their 
        /// associated descriptions.
        /// </summary>
        /// <value>The usage instructions.</value>
        public virtual string Usage
        {
            get
            {
                return GetUsage(null, -1);
            }
        }

        /// <summary>
        /// Gets a value indicating if command line processing is case sensitive
        /// </summary>
        public bool CaseSensitive
        {
            get
            {
                return (flags & CommandLineParserFlags.CaseSensitive) != 0;
            }
        }

        /// <summary>
        /// Gets or sets the copyright information to display in the logo. This value is automatically obtained from either 
        /// <see cref="CommandLineCopyrightAttribute"/> or <see cref="AssemblyCopyrightAttribute"/> that is specified for the assembly 
        /// containing the argument specification type. 
        /// </summary>
        public string Copyright
        {
            get
            {
                if (copyright == null)
                {
                    object[] copyrightAttributes = argTargetType.GetCustomAttributes(typeof(CommandLineCopyrightAttribute), true);

                    if (copyrightAttributes.Length > 0)
                    {
                        CommandLineCopyrightAttribute copyrightAttribute = (CommandLineCopyrightAttribute)copyrightAttributes[0];

                        // This one is localizable
                        copyright = ExternalGetString(copyrightAttribute.Copyright);
                    }
                    else
                    {
                        copyrightAttributes = argTargetType.Assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), true);

                        if (copyrightAttributes.Length > 0)
                        {
                            AssemblyCopyrightAttribute copyrightAttribute = (AssemblyCopyrightAttribute)copyrightAttributes[0];

                            // This one is just used literally
                            copyright = copyrightAttribute.Copyright;
                        }
                        else
                        {
                            throw new ArgumentException(CommandLineParserResources.CopyrightAttributesNotFound);
                        }
                    }
                }

                return copyright.Replace("\xA9", "(c)");
            }

            set
            {
                copyright = value;
            }
        }

        /// <summary>
        /// Gets or sets the configuration string to display in the logo.  Usually set to Debug for debug builds and
        /// left empty for release builds.  This value is automatically obtained from the <see cref="AssemblyConfigurationAttribute"/> 
        /// that is specified for the assembly containing the argument specification type.
        /// </summary> 
        public string Configuration
        {
            get
            {
                if (configuration == null)
                {
                    object[] configurationAttributes = argTargetType.GetCustomAttributes(typeof(CommandLineConfigurationAttribute), true);

                    if (configurationAttributes.Length > 0)
                    {
                        CommandLineConfigurationAttribute configurationAttribute = (CommandLineConfigurationAttribute)configurationAttributes[0];

                        // This is localizable
                        configuration = ExternalGetString(configurationAttribute.Configuration);
                    }
                    else
                    {
                        configurationAttributes = argTargetType.Assembly.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), true);

                        if (configurationAttributes.Length > 0)
                        {
                            AssemblyConfigurationAttribute configurationAttribute = (AssemblyConfigurationAttribute)configurationAttributes[0];

                            // This is literal
                            configuration = configurationAttribute.Configuration;
                        }
                        else
                        {
                            configuration = String.Empty;
                        }
                    }
                }

                return configuration;
            }

            set
            {
                configuration = value;
            }
        }

        /// <summary>
        /// Gets or sets the version string to display in the logo.  Set by default to the version of the entry assembly or 
        /// executing assembly if the entry assembly has no version. This value is automatically obtained from the <see cref="AssemblyFileVersionAttribute"/> 
        /// that is specified for the assembly containing the argument specification type.
        /// </summary>
        public string Version
        {
            get
            {
                if (version == null)
                {
                    Assembly assembly = argTargetType.Assembly;
                    object[] versionAttributes = assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true);

                    if (versionAttributes.Length > 0)
                    {
                        AssemblyFileVersionAttribute versionAttribute = (AssemblyFileVersionAttribute)versionAttributes[0];

                        version = versionAttribute.Version;
                    }
                    else
                    {
                        throw new ArgumentException(CommandLineParserResources.VersionAttributesNotFound);
                    }
                }

                return version;
            }

            set
            {
                version = value;
            }
        }

        /// <summary>
        /// Gets or sets the description string to display in the logo.  This value is automatically obtained from the <see cref="CommandLineDescriptionAttribute"/>
        /// if it exists in the assembly containing the argument specification type.  If it does not then <see cref="AssemblyDescriptionAttribute"/> is used
        /// from the same assembly.
        /// </summary>
        public string Description
        {
            get
            {
                if (description == null)
                {
                    object[] descriptionAttributes = argTargetType.GetCustomAttributes(typeof(CommandLineDescriptionAttribute), true);

                    if (descriptionAttributes.Length > 0)
                    {
                        CommandLineDescriptionAttribute descriptionAttribute = (CommandLineDescriptionAttribute)descriptionAttributes[0];

                        // This value can be localized
                        description = ExternalGetString(descriptionAttribute.Description);
                    }
                    else
                    {
                        descriptionAttributes = argTargetType.Assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), true);

                        if (descriptionAttributes.Length > 0)
                        {
                            AssemblyDescriptionAttribute descriptionAttribute = (AssemblyDescriptionAttribute)descriptionAttributes[0];

                            // Use this value literally
                            description = descriptionAttribute.Description;
                        }
                        else
                        {
                            throw new ArgumentException(CommandLineParserResources.DescriptionAttributesNotFound);
                        }
                    }
                }

                return description;
            }

            set
            {
                description = value;
            }
        }

        /// <summary>
        /// Gets or sets the description string to display in the logo.  This value is automatically obtained from the <see cref="CommandLineDescriptionAttribute"/>
        /// if it exists in the assembly containing the argument specification type.  If it does not then <see cref="AssemblyDescriptionAttribute"/> is used
        /// from the same assembly.
        /// </summary>
        public string Title
        {
            get
            {
                if (title == null)
                {
                    object[] titleAttributes = argTargetType.GetCustomAttributes(typeof(CommandLineTitleAttribute), true);

                    if (titleAttributes.Length > 0)
                    {
                        CommandLineTitleAttribute titleAttribute = (CommandLineTitleAttribute)titleAttributes[0];

                        // This value can be localized
                        title = ExternalGetString(titleAttribute.Title);
                    }
                    else
                    {
                        titleAttributes = argTargetType.Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), true);

                        if (titleAttributes.Length > 0)
                        {
                            AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)titleAttributes[0];

                            // Use this value literally
                            title = titleAttribute.Title;
                        }
                        else
                        {
                            throw new ArgumentException(CommandLineParserResources.TitleAttributesNotFound);
                        }
                    }
                }

                return title;
            }

            set
            {
                title = value;
            }
        }

        /// <summary>
        /// Gets a boolean indicating if the parser has a default command line argument.
        /// </summary>
        public bool HasDefaultArgument
        {
            get
            {
                return defaultArgument != null;
            }
        }

        /// <summary>
        /// Gets a boolean indicating if the parser has an unprocessed command line argument.
        /// </summary>
        public bool HasUnprocessedArgument
        {
            get
            {
                return unprocessedArgument != null;
            }
        }

        /// <summary>
        /// Gets a flag indicating if the parser has a a command argument.
        /// </summary>
        public bool HasCommandArgument
        {
            get
            {
                return commandArgument != null;
            }
        }

        /// <summary>
        /// Gets the argument extracted from the target object.
        /// </summary>
        /// <value>The argument collection.</value>
        public IEnumerable<CommandLineArgument> ArgumentCollection
        {
            get 
            {
                return argCollection;
            }
        }

        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineParser" /> class.  Provides a resource reader for localization of the command 
        /// line arguments.  String specified in the command line attributes are used to look up corresponding localized strings in the resources.
        /// Also provides flags to control parsing.
        /// </summary>
        /// <param name="argTarget">The <see cref="object"/> into which the command line arguments are placed.</param>
        /// <param name="resourceReaderType">A resource reader object with a <code>GetString</code> method</param>
        /// <param name="flags">See <see cref="CommandLineParserFlags"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="argTargetType" /> is a null reference.</exception>
        public CommandLineParser(object argTarget, Type resourceReaderType = null, CommandLineParserFlags flags = CommandLineParserFlags.Default)
        {
            if (argTarget == null)
            {
                throw new ArgumentNullException("argTarget");
            }

            this.argTargetType = argTarget.GetType();
            this.resourceReaderType = resourceReaderType;
            this.flags = flags;
            this.responseFiles = new Stack<string>();
            this.setStates = new Dictionary<string, bool>();

            argCollection = new List<CommandLineArgument>();

            // Look ONLY for public instance properties.  NOTE: Don't change this behavior!
            foreach (PropertyInfo propertyInfo in argTargetType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                object[] attributes = propertyInfo.GetCustomAttributes(typeof(CommandLineArgumentAttribute), true);
                CommandLineArgumentAttribute attribute;

                // Ignore properties that don't have a CommandLineArgumentAttribute
                if (attributes.Length == 1)
                    attribute = (CommandLineArgumentAttribute)attributes[0];
                else
                    continue;

                // Ensure that the property is readable and writeable
                if (!(propertyInfo.CanWrite && propertyInfo.CanRead))
                    throw new ArgumentException(
                        CommandLineParserResources.PropertyShouldBeReadableAndWriteable(propertyInfo.Name));

                attribute.ValueHint = ExternalGetString(attribute.ValueHint);
                CommandLineArgument arg = null;

                if (attribute is DefaultCommandLineArgumentAttribute)
                {
                    if (HasDefaultArgument)
                        throw new ArgumentException(CommandLineParserResources.DefaultArgumentAlreadyDefined);
                    
                    arg = defaultArgument = new CommandLineArgument(argTarget, attribute, propertyInfo);
                }
                else if (attribute is UnprocessedCommandLineArgumentAttribute)
                {
                    if (HasUnprocessedArgument)
                        throw new ArgumentException(CommandLineParserResources.UnprocessedArgumentAlreadyDefined);
                        
                    arg = unprocessedArgument = new CommandLineArgument(argTarget, attribute, propertyInfo);
                    
                    if (!unprocessedArgument.AllowsMultiple)
                        throw new ArgumentException(CommandLineParserResources.UnprocessedArgumentMustBeArrayOrCollection);
                }
                else if (attribute is CommandCommandLineArgumentAttribute)
                {
                    if (HasCommandArgument)
                        throw new ArgumentException(CommandLineParserResources.CommandArgumentAlreadyDefined);
                        
                    arg = commandArgument = new CommandLineArgument(argTarget, attribute, propertyInfo);
                }
                else
                {
                    attribute.Description = ExternalGetString(attribute.Description);
                    
                    // If not being case sensitive, make everything lower case.
                    if (!CaseSensitive)
                    {
                        attribute.Name = attribute.Name.ToLower(CultureInfo.InvariantCulture);
                    
                        if (attribute.ShortName != null)
                            attribute.ShortName = attribute.ShortName.ToLower(CultureInfo.InvariantCulture);
                    }
                
                    arg = new CommandLineArgument(argTarget, attribute, propertyInfo);
                }

                argCollection.Add(arg);
                setStates.Add(arg.Name, false);
            }
            
            if (HasUnprocessedArgument && !HasDefaultArgument)
            {
                throw new ArgumentException(CommandLineParserResources.UnprocessedRequiresDefaultArguments);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Parses the supplied command line arguments and sets the target objects properties.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="target"></param>
        public void ParseAndSetTarget(IEnumerable<string> argStrings)
        {
            foreach (var arg in argCollection)
                setStates[arg.Name] = false;

            if (argStrings == null || argStrings.Count() == 0)
                return;

            foreach (string argString in argStrings)
            {
                if (argString.Length == 0)
                    continue;

                if (HasDefaultArgument && defaultArgument.HasValue && HasUnprocessedArgument)
                {
                    // We have a default argument now, everything else should be left unprocessed
                    unprocessedArgument.ParseAndSetTarget(argString);
                    setStates[unprocessedArgument.Name] = true;
                    continue;
                }

                switch (argString[0])
                {
                case '@':
                    {
                        // handle response file
                        if (responseFiles.Count >= maxResponseFileDepth)
                        {
                            StringWriter error = new StringWriter(CultureInfo.CurrentCulture);
                            error.WriteLine(CommandLineParserResources.ResponseFilesTooDeep);

                            foreach (string fname in responseFiles.ToArray())
                                error.WriteLine("\t@" + fname);

                            throw new CommandLineArgumentException(error.ToString());
                        }
                        string path = argString.Substring(1);
                        try
                        {
                            string rspFileName = Path.GetFullPath(path);
                            using (TextReader rspFile = new StreamReader(rspFileName))
                            {
                                // Read all the lines as one argument per line
                                List<string> responseArgs = new List<string>();
                                string nextLine = null;

                                while ((nextLine = rspFile.ReadLine()) != null)
                                    responseArgs.Add(nextLine);

                                // Recursively call parse; any exceptions will be propagated up
                                responseFiles.Push(rspFileName);
                                ParseAndSetTarget(responseArgs);
                                responseFiles.Pop();
                            }
                        }
                        catch (CommandLineArgumentException)
                        {
                            // As soon as we get one of these propagate it upwards
                            throw;
                        }
                        catch (ArgumentException ex)
                        {
                            throw new CommandLineArgumentException(
                                CommandLineParserResources.ResponseFileUnopened(path), ex);
                        }
                        catch (IOException ex)
                        {
                            throw new CommandLineArgumentException(
                                CommandLineParserResources.ResponseFileUnopened(path), ex);
                        }

                        break;
                    }
                case '-':
#if WINDOWS
                case '/':
#endif
                    int endIndex = argString.IndexOfAny(new char[] { ':', '+', '-' }, 1);
                    string argumentName = argString.Substring(1, endIndex == -1 ? argString.Length - 1 : endIndex - 1);
                    string argumentValue;

                    if (argumentName.Length + 1 == argString.Length)
                    {
                        // There's nothing left for a value
                        argumentValue = null;
                    }
                    else if (argString.Length > 1 + argumentName.Length && argString[1 + argumentName.Length] == ':')
                    {
                        argumentValue = argString.Substring(argumentName.Length + 2);
                    }
                    else
                    {
                        argumentValue = argString.Substring(argumentName.Length + 1);
                    }

                    if (!CaseSensitive)
                    {
                        argumentName = argumentName.ToLower(CultureInfo.InvariantCulture);
                    }

                    CommandLineArgument arg;

                    // By this point the strings have either been made the same case or not, and 
                    // we only care about equality, so an ordinal comparison is fine.
                    if (HasCommandArgument)
                    {
                        string command;
                            
                        // If the cammand has not yet been set then the command string is assumed to be default
                        // or empty command, otherwise it is whatever the command value is.
                        if (HasCommandArgument)
                            command = commandArgument.ToString().ToLower(CultureInfo.InvariantCulture);
                        else
                            command = String.Empty;
                        
                        // Needed for the use of the parser in the lambda
                        CommandLineParser parser = this;

                        arg = argCollection.Find(
                            item =>
                            {
                                return
                                    parser.IsValidArgumentForCommand(item, command) &&
                                    String.CompareOrdinal(item.Name, argumentName) == 0 ||
                                    String.CompareOrdinal(item.ShortName, argumentName) == 0;
                            });
                    }
                    else
                    {
                        arg = argCollection.Find(
                            item =>
                            {
                                return
                                    String.CompareOrdinal(item.Name, argumentName) == 0 ||
                                    String.CompareOrdinal(item.ShortName, argumentName) == 0;
                            });
                    }

                    if (arg == null)
                    {
                        throw new CommandLineArgumentException(CommandLineParserResources.UnknownArgument(argumentName));
                    }
                    else
                    {
                        if (!arg.AllowsMultiple && setStates[arg.Name])
                            throw new CommandLineArgumentException(CommandLineParserResources.DuplicateCommandLineArgument(arg.Name));

                        arg.ParseAndSetTarget(argumentValue);
                        setStates[arg.Name] = true;
                    }
                    break;
                default:
                    if (HasCommandArgument && !commandArgument.HasValue)
                    {
                        string command = argString.ToLower(CultureInfo.InvariantCulture);
                            
                        if (!IsValidCommand(command))
                        {
                            throw new CommandLineArgumentException(CommandLineParserResources.UnknownCommandArgument(command));
                        }
                            
                        commandArgument.ParseAndSetTarget(command);
                        setStates[commandArgument.Name] = true;
                    }
                    else if (HasDefaultArgument)
                    {
                        if (!defaultArgument.AllowsMultiple && setStates[defaultArgument.Name])
                            throw new CommandLineArgumentException(CommandLineParserResources.DuplicateCommandLineArgument(defaultArgument.Name));

                        defaultArgument.ParseAndSetTarget(argString);
                        setStates[defaultArgument.Name] = true;
                    }
                    else
                    {
                        throw new CommandLineArgumentException(CommandLineParserResources.UnknownArgument(argString));
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Gets the usage string for a specific command.
        /// </summary>
        /// <param name="command">The command for which to obtain usage.  Pass <value>null</value> to obtain default usage.</param>
        /// <returns>The usage string formatted for the default number of display columns.</returns>
        public string GetUsage(string command)
        {
            return GetUsage(command, -1);
        }

        /// <summary>
        /// Gets the usage string for a specific command. 
        /// </summary>
        /// <param name="command">The command for which to obtain usage.  Pass <value>null</value> to obtain default usage.</param>
        /// <param name="lineLength">
        /// Number of columns to format the output for.  Pass <value>-1</value> to format for the active console.  
        /// If there is no console window the default will be 79.  Any line length below 39 is rounded up to 39.
        /// </param>
        /// <returns>The usage string formatted for the specified number of display columns.</returns>
        public string GetUsage(string command, int lineLength)
        {
            StringBuilder helpText = new StringBuilder();
            
            // The empty string is the 'default' command.  This allows some arguments to be valid even if there is no command.  
            if (command == null)
                command = String.Empty;

            command = command.Trim();

            if (lineLength == -1)
            {
                lineLength = 79;

#if WINDOWS
                // There may not be a console window.  Sadly, there is no way to check if there is or not!
                try
                {
                    lineLength = Console.BufferWidth;
                }
                catch (IOException) { }
#endif
            }
            else
            {
                lineLength = Math.Max(lineLength, 40);
            }

            if (HasCommandArgument && !IsValidCommand(command))
            {
                throw new CommandLineArgumentException(CommandLineParserResources.UnknownCommandArgument(command));
            }

            GetSyntaxHelpLine(command, helpText);
            GetDescriptionHelpLines(command, lineLength, helpText);

            if (HasCommandArgument && command.Length == 0)
                GetCommandHelpLines(lineLength, helpText);
            else
                GetSwitchesHelpLines(command, lineLength, helpText);

            if (command.Length == 0)
                GetExampleHelpLines(lineLength, helpText);

            // Remove any trailing newline; let the caller add them if they want too.
            return helpText.ToString().TrimEnd('\r', '\n');
        }

        /// <summary>
        /// Gets all set arguments in the target as a valid command line string
        /// </summary>
        public string GetArgumentString()
        {
            StringBuilder sb = new StringBuilder();
            List<CommandLineArgument>.Enumerator enumerator = argCollection.GetEnumerator();

            if (HasCommandArgument && commandArgument.HasValue)
            {
                sb.Append(" " + commandArgument.ToString());
            }

            while (enumerator.MoveNext())
            {
                CommandLineArgument argument = enumerator.Current;

                if (!HasCommandArgument || argument.IsValidForCommand(commandArgument.ToString()))
                {
                    string s = argument.ToString();

                    if (s.Length > 0)
                        sb.AppendFormat(" " + s);
                }
            }

            return sb.ToString();
        }

        #endregion

        #region Private Methods

        private void GetSyntaxHelpLine(string command, StringBuilder helpText)
        {
            Debug.Assert(command != null);  // ... but zero length command is OK

            helpText.AppendFormat("{0,-22}{1}", CommandLineParserResources.Syntax_Propercase, 
                this.CommandName);

            if (HasCommandArgument && command.Length == 0)
            {
                helpText.Append(" <" + CommandLineParserResources.Command_Lowercase + ">");

                // Just give a rough indication that there may or may not be more arguments
                if (HasArguments(command) || HasDefaultArgument || HasUnprocessedArgument)
                {
                    helpText.Append(" ...");
                }
            }
            else
            {
                if (command.Length > 0)
                    helpText.Append(" " + command);
    
                if (HasArguments(command))
                {
                    helpText.Append(" [" + CommandLineParserResources.Switches_Lowercase + "]");
                }
    
                if (HasDefaultArgument && IsValidArgumentForCommand(defaultArgument, command))
                {
                    string hint = GetDefaultArgumentValueHint(command);
    
                    if (hint == null)
                    {
                        if (!String.IsNullOrEmpty(defaultArgument.ValueHint))
                            hint = defaultArgument.ValueHint;
                        else
                            hint = "<" + defaultArgument.Name.ToLower(CultureInfo.InvariantCulture) + ">";
                    }
    
                    helpText.Append(" " + hint);
    
                    // We can only have unprocessed arguments with a default argument
                    if (!HasUnprocessedArgument)
                    {
                        if (defaultArgument.AllowsMultiple)
                        {
                            // Add additional default arguments
                            helpText.Append(" [");
                            helpText.Append(hint);
                            helpText.Append(" ...]");
                        }
                    }
                    else if (IsValidArgumentForCommand(unprocessedArgument, command))
                    {
                        // Add the unprocessed arguments hint
                        hint = GetUnprocessedArgumentValueHint(command);
    
                        if (hint == null)
                        {
                            if (!String.IsNullOrEmpty(unprocessedArgument.ValueHint))
                                hint = unprocessedArgument.ValueHint;
                            else
                                hint = "<" + unprocessedArgument.Name.ToLower(CultureInfo.InvariantCulture) + ">";
                        }
    
                        // Indicate that there can be more unprocessed arguments
                        helpText.Append(" [");
                        helpText.Append(hint);
                        helpText.Append(" ...]");
                    }
                }
            }

            helpText.Append(Environment.NewLine + Environment.NewLine);
        }

        private void GetCommandHelpLines(int lineLength, StringBuilder helpText)
        {
            // Add switches to help text
            helpText.AppendFormat("{0}{1}{1}", CommandLineParserResources.Commands_Propercase, System.Environment.NewLine);

            object[] attributes = this.argTargetType.GetCustomAttributes(typeof(CommandLineCommandDescriptionAttribute), true);
            List<string> commands = new List<string>(attributes.Length);
            
            // Build a list of all the commands
            foreach (CommandLineCommandDescriptionAttribute attribute in attributes)
            {
                commands.Add(attribute.Command);
            }

            string indent = new string(' ', 22);

            foreach (CommandLineCommandDescriptionAttribute attribute in attributes)
            {
                string command = attribute.Command;
                string description = ExternalGetString(attribute.Description);

                if (description == null)
                    description = "";

                string[] descriptionLines = StringUtility.WordWrap(description, (lineLength - 1) - 22);
                if (descriptionLines.Length == 0)
                    descriptionLines = new string[] { "" };

                for (int i = 0; i < descriptionLines.Length; i++)
                {
                    if (i == 0)
                    {
                        if (command.Length > 22 - 3)
                            helpText.AppendFormat(CultureInfo.CurrentCulture, "  {0}{1}{2}{3}{1}", command, Environment.NewLine, indent, descriptionLines[i]);
                        else
                            helpText.AppendFormat(CultureInfo.CurrentCulture, "  {0}{1}{2}{3}", command, indent.Substring(0, 22 - 2 - command.Length), descriptionLines[i], Environment.NewLine);
                    }
                    else
                        helpText.AppendFormat("{0}{1}{2}", indent, descriptionLines[i], Environment.NewLine);
                }

                helpText.AppendLine();
            }
        }

        private void GetSwitchesHelpLines(string command, int lineLength, StringBuilder helpText)
        {
            Debug.Assert(command != null);
            
            if (!HasArguments(command))
                return;
            
            // Add switches to help text
            helpText.AppendFormat("{0}{1}{1}", CommandLineParserResources.Switches_Propercase, System.Environment.NewLine);

            // First build an array of all the switches with all the trimmings,
            // and the descriptions with any necessary modifications, such as the 
            // short switch.
            List<KeyValuePair<CommandLineArgument, string>> switches =
                new List<KeyValuePair<CommandLineArgument, string>>(argCollection.Count);

            foreach (CommandLineArgument argument in argCollection)
            {
                if (!IsValidArgumentForCommand(argument, command))
                    continue;
                
                string valueText = "";

                if (argument.ValueHint != null)
                {
                    if (argument.ValueHint != String.Empty)
                        valueText = ":" + argument.ValueHint;
                }
                else if (argument.ArgType == typeof(string))
                {
                    valueText = ":<" + CommandLineParserResources.Text_LowerCase + ">";
                }
                else if (
                    argument.ArgType == typeof(FileInfo) ||
                    argument.ArgType == typeof(ParsedPath))
                {
                    valueText = ":<" + CommandLineParserResources.FileName_Lowercase + ">";
                }
                else if (
                    argument.ArgType == typeof(int) ||
                    argument.ArgType == typeof(uint) ||
                    argument.ArgType == typeof(short) ||
                    argument.ArgType == typeof(ushort))
                {
                    valueText = ":<" + CommandLineParserResources.Number_Lowercase + ">";
                }
                else if (argument.ArgType != typeof(bool))
                {
                    valueText = ":<" + CommandLineParserResources.Value_Lowercase + ">";
                }

                string name = argument.Name;
                string shortName = argument.ShortName;

                if (shortName != null && name.StartsWith(shortName, StringComparison.InvariantCulture))
                {
                    name = name.Insert(shortName.Length, "[") + "]";
                }

                string sw = "-" + name + valueText;

                switches.Add(new KeyValuePair<CommandLineArgument,string>(argument, sw));
            }

            // Now go through and append each line of the help text
            foreach (KeyValuePair<CommandLineArgument, string> pair in switches)
            {
                CommandLineArgument argument = pair.Key;
                string sw = pair.Value;
                string description = argument.Description;
                string name = argument.Name;
                string shortName = argument.ShortName;

                if (shortName != null && !name.StartsWith(shortName, StringComparison.InvariantCulture))
                {
                    description += StringUtility.CultureFormat(
                        (description.Length > 0 && !description.EndsWith(" ", StringComparison.CurrentCulture) ? " " : "") +
                        CommandLineParserResources.ShortFormat(shortName));
                }

                string[] descriptionLines = StringUtility.WordWrap(description == null ? "" : description, (lineLength - 1) - 22);

                for (int i = 0; i < descriptionLines.Length; i++)
                {
                    if (i == 0)
                    {
                        if (sw.Length > 22 - 3)
                            helpText.AppendFormat(CultureInfo.CurrentCulture, "  {0}{1}{2,22}{3}{1}", sw, Environment.NewLine, String.Empty, descriptionLines[i]);
                        else
                            helpText.AppendFormat(CultureInfo.CurrentCulture, "  {0,-20}{1}{2}", sw, descriptionLines[i], Environment.NewLine);
                    }
                    else
                        helpText.AppendFormat("{0,22}{1}{2}", String.Empty, descriptionLines[i], Environment.NewLine);
                }

                helpText.AppendLine();
            }
        }

        private void GetDescriptionHelpLines(string command, int lineLength, StringBuilder helpText)
        {
            string description = null;

            if (command.Length != 0)
            {
                if (commandArgument != null && command.Length != 0)
                {
                    // Print the command description
                    object[] attributes = this.argTargetType.GetCustomAttributes(typeof(CommandLineCommandDescriptionAttribute), true);

                    foreach (CommandLineCommandDescriptionAttribute attribute in attributes)
                    {
                        if (String.Compare(attribute.Command, command, StringComparison.InvariantCulture) == 0)
                        {
                            description = ExternalGetString(attribute.Description);
                            break;
                        }
                    }
                }
            }
            else
            {
                object[] attributes = this.argTargetType.GetCustomAttributes(typeof(CommandLineDescriptionAttribute), true);

                if (attributes.Length > 0)
                {
                    CommandLineDescriptionAttribute attribute = (CommandLineDescriptionAttribute)attributes[0];

                    description = ExternalGetString(attribute.Description);
                }
            }

            if (description == null)
                return;

            string[] descriptionLines = StringUtility.WordWrap(description, (lineLength - 1) - 22);
            string indent = new string(' ', 22);

            for (int i = 0; i < descriptionLines.Length; i++)
            {
                if (i == 0)
                    helpText.AppendFormat("{0,-22}{1}{2}", CommandLineParserResources.Description_Propercase, descriptionLines[i], Environment.NewLine);
                else
                    helpText.AppendFormat("{0}{1}{2}", indent, descriptionLines[i], Environment.NewLine);
            }

            helpText.AppendLine();
        }

        private void GetExampleHelpLines(int lineLength, StringBuilder helpText)
        {
            object[] attributes = this.argTargetType.GetCustomAttributes(typeof(CommandLineExampleAttribute), true);

            if (attributes.Length == 0)
                return;

            CommandLineExampleAttribute attribute = (CommandLineExampleAttribute)attributes[0];

            string example = ExternalGetString(attribute.Example);
            string[] exampleLines = StringUtility.WordWrap(example, (lineLength - 1) - 22);

            helpText.AppendFormat("{0}{1}{1}", CommandLineParserResources.Examples_Propercase, Environment.NewLine);

            for (int i = 0; i < exampleLines.Length; i++)
            {
                helpText.AppendFormat("  {0}{1}", exampleLines[i], Environment.NewLine);
            }

            helpText.AppendLine();
        }

        // Does a command have arguments (not including the default or unprocessed arguments)
        private bool HasArguments(string command)
        {
            Debug.Assert(command != null);

            if (command.Length == 0)
            {
                return argCollection.Count > 0;
            }
            else
            {
                foreach (CommandLineArgument argument in argCollection)
                {
                    if (IsValidArgumentForCommand(argument, command))
                        return true;
                }

                return false;
            }
        }

        private string GetDefaultArgumentValueHint(string command)
        {
            Debug.Assert(command != null);

            object[] attributes = this.argTargetType.GetCustomAttributes(typeof(CommandLineCommandDescriptionAttribute), true);

            foreach (CommandLineCommandDescriptionAttribute attribute in attributes)
            {
                if (String.Compare(attribute.Command, command, StringComparison.InvariantCulture) == 0)
                {
                    return ExternalGetString(attribute.DefaultValueHint);
                }
            }

            return null;
        }

        private string GetUnprocessedArgumentValueHint(string command)
        {
            Debug.Assert(command != null);

            object[] attributes = this.argTargetType.GetCustomAttributes(typeof(CommandLineCommandDescriptionAttribute), true);

            foreach (CommandLineCommandDescriptionAttribute attribute in attributes)
            {
                if (String.Compare(attribute.Command, command, StringComparison.InvariantCulture) == 0)
                {
                    return ExternalGetString(attribute.UnprocessedValueHint);
                }
            }

            return null;
        }

        private string ExternalGetString(string s)
        {
            // If we don't have a resource reader or the string is null/empty just return the string
            if (resourceReaderType == null || s == null || s == String.Empty)
                return s;
        
            // Look for a static property in the resource reader that has the name in 's'
            PropertyInfo propInfo = resourceReaderType.GetProperty(s, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            
            if (propInfo == null)
                throw new ArgumentException(
                    CommandLineParserResources.ResourceReaderDoesNotContainString(resourceReaderType.ToString(), s));

            object obj = propInfo.GetValue(null, null);
            string s2 = obj as String;

            if (s2 != null)
                return s2;
            else
                return obj.ToString();
        }

        /// <summary>
        /// Is this a valid command.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private bool IsValidCommand(string command)
        {
            if (HasCommandArgument && commandArgument.IsValidForCommand(command))
                return true;

            LazyGenerateCommandArguments();

            return commandArguments.ContainsKey(command);
        }

        /// <summary>
        /// Is the argument valid of for this command?
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        private bool IsValidArgumentForCommand(CommandLineArgument argument, string command)
        {
            if (argument.IsValidForCommand(command))
                return true;

            LazyGenerateCommandArguments();

            return Array.Exists(commandArguments[command],
                delegate(string s)
                {
                    return string.CompareOrdinal(s, argument.Name) == 0;
                });
        }

        private void LazyGenerateCommandArguments()
        {
            if (commandArguments == null)
            {
                object[] attributes = this.argTargetType.GetCustomAttributes(typeof(CommandLineCommandDescriptionAttribute), true);
                commandArguments = new Dictionary<string, string[]>(attributes.Length);

                // Build a list of all the commands
                foreach (CommandLineCommandDescriptionAttribute attribute in attributes)
                {
                    string[] switches = attribute.Switches != null ? attribute.Switches.Split(',') : new string[0];
                    commandArguments.Add(attribute.Command, switches);
                }
            }
        }


        #endregion
    }

    /// <summary>
    /// The exception that is thrown when one of the command line arguments provided 
    /// is not valid.
    /// </summary>
    [Serializable]
    public sealed class CommandLineArgumentException : ArgumentException
    {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentException" /> class.
        /// </summary>
        public CommandLineArgumentException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentException" /> class
        /// with a descriptive message.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        public CommandLineArgumentException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentException" /> class
        /// with a descriptive message and an inner exception.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        /// <param name="innerException">A nested exception that is the cause of the current exception.</param>
        public CommandLineArgumentException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private CommandLineArgumentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }

    /// <summary>
    /// Defines a title for the command line tool.  This title is displayed in the banner.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class CommandLineTitleAttribute : Attribute
    {
        #region Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineDescriptionAttribute" />.
        /// </summary>
        /// <param name="title">Description of the command line program</param>
        public CommandLineTitleAttribute(string title)
        {
            Title = title;
        }

        #endregion

        #region Instance Properties

        /// <summary>
        /// Gets or sets the title of the command line tool.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Private resource reader
        /// </summary>
        public Type ResourceReader { get; private set; }

        #endregion
    }

    /// <summary>
    /// Defines a description for the command line tool.  This description is displayed in the help text.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class CommandLineDescriptionAttribute : Attribute
    {
        #region Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineDescriptionAttribute" />.
        /// </summary>
        /// <param name="description">Description of the command line program</param>
        public CommandLineDescriptionAttribute(string description)
        {
            Description = description;
        }

        #endregion

        #region Instance Properties

        /// <summary>
        /// Gets or sets the description of the command line tool.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Private resource reader
        /// </summary>
        public Type ResourceReader { get; set; }

        #endregion
    }

    /// <summary>
    /// Defines examples for the command line tool.  The examples are displayed in the help text.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class CommandLineExampleAttribute : Attribute
    {
        #region Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineDescriptionAttribute" />.
        /// </summary>
        /// <param name="example">Description of the command line program</param>
        public CommandLineExampleAttribute(string example)
        {
            Example = example;
        }

        #endregion

        #region Instance Properties

        /// <summary>
        /// Gets or sets the description of the command line tool.
        /// </summary>
        public string Example { get; private set; }

        /// <summary>
        /// Private resource reader
        /// </summary>
        public Type ResourceReader { get; set; }

        #endregion
    }

    /// <summary>
    /// Defines a general description for the command line tool
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class CommandLineCommandDescriptionAttribute : Attribute
    {
        #region Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineCommandDescriptionAttribute" />.
        /// </summary>
        /// <param name="command">Name of the command line program</param>
        public CommandLineCommandDescriptionAttribute(string command)
        {
            Command = command;
        }

        #endregion

        #region Instance Properties

        /// <summary>
        /// Gets or sets the description of the command line command.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the command name
        /// </summary>
        public string Command { get; private set; }

        /// <summary>
        /// Gets or sets the default argument value hint override
        /// </summary>
        public string DefaultValueHint { get; set; }

        /// <summary>
        /// Gets or sets the unprocessed argument value hint override
        /// </summary>
        public string UnprocessedValueHint { get; set; }

        /// <summary>
        /// Private resource reader
        /// </summary>
        public Type ResourceReader { get; set; }

        /// <summary>
        /// Comma-separated string of switches that apply to this command
        /// </summary>
        public string Switches { get; set; }

        #endregion
    }

    /// <summary>
    /// Defines a copyright for a command line tool
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class CommandLineCopyrightAttribute : Attribute
    {
        #region Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineCopyrightAttribute" />.
        /// </summary>
        /// <param name="copyright">Copyright for the program</param>
        public CommandLineCopyrightAttribute(string copyright)
        {
            Copyright = copyright;
        }

        #endregion

        #region Instance Properties

        /// <summary>
        /// Gets or sets the copyright argument.
        /// </summary>
        /// <value>The program copyright information.</value>
        public string Copyright { get; private set; }

        /// <summary>
        /// Private resource reader
        /// </summary>
        public Type ResourceReader { get; set; }

        #endregion
    }

    /// <summary>
    /// Defines a build configuration for a command line tool
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class CommandLineConfigurationAttribute : Attribute
    {
        #region Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineConfigurationAttribute" />.
        /// </summary>
        /// <param name="configuration">Copyright for the program</param>
        public CommandLineConfigurationAttribute(string configuration)
        {
            Configuration = configuration;
        }

        #endregion

        #region Instance Properties

        /// <summary>
        /// Gets or sets the configuration name.
        /// </summary>
        /// <value>The program configuration name (usually Debug or Release).</value>
        public string Configuration { get; private set; }

        /// <summary>
        /// Private resource reader
        /// </summary>
        public Type ResourceReader { get; set; }

        #endregion
    }

    /// <summary>
    /// Defines a property as an command line argument 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CommandLineArgumentAttribute : Attribute
    {
        #region Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentAttribute" /> class
        /// with the specified argument type.
        /// </summary>
        /// <param name="name">Specifies the checking to be done on the argument.</param>
        public CommandLineArgumentAttribute(string name)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            this.Name = name;
            this.MethodName = "Parse"; // Default
        }

        #endregion

        #region Instance Properties

        /// <summary>
        /// Gets or sets the long name of the argument.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the short name of the argument.
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// Gets or sets the description of the argument.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets hint text to describe the argument value.    Setting this to an empty string
        /// can be used to disable the default value hint, such as [+|-] for boolean arguments.
        /// </summary>
        public string ValueHint { get; set; }

        /// <summary>
        /// Gets or sets the initializer type.  This is a class that contains a <code>object Parse(object value)</code> method that 
        /// is passed the command line value and returns a value compatible with the target property.  The name of this method 
        /// can be overridden with the <see cref="MethodName"/> property.
        /// </summary>
        public Type Initializer { get; set; }

        /// <summary>
        /// Gets or sets the initializer method name.  The method must be static, take a single <code>string</code> parameter and 
        /// return a value that is the same type as the target command line property.
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// Gets or sets the commands associated with this argument.  Commands are specified as a comma separated list.
        /// </summary>
        public string Commands { get; set; }

        #endregion
    }

    /// <summary>
    /// Used to capture arguments that are not switches.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class DefaultCommandLineArgumentAttribute : CommandLineArgumentAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultCommandLineArgumentAttribute"/>
        /// </summary>
        /// <param name="name">Name of the default attribute</param>
        public DefaultCommandLineArgumentAttribute() : base("default")
        {
        }
    }

    /// <summary>
    /// Used to capture the remainder of a command line after other arguments have been processed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class UnprocessedCommandLineArgumentAttribute : CommandLineArgumentAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnprocessedCommandLineArgumentAttribute" /> 
        /// </summary>
        public UnprocessedCommandLineArgumentAttribute() : base("unprocessed")
        {
        }
    }

    /// <summary>
    /// Used to for command based command lines where one of the arguments is is a command. The command defines 
    /// which of the other arguments are valid for that command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class CommandCommandLineArgumentAttribute : CommandLineArgumentAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandCommandLineArgumentAttribute" /> 
        /// </summary>
        public CommandCommandLineArgumentAttribute()
            : base("command")
        {
        }
    }

    /// <summary>
    /// Flags that control command line parsing
    /// </summary>
    [Flags]
    public enum CommandLineParserFlags
    {
        /// <summary>
        /// All switches are matched case sensitively.
        /// </summary>
        CaseSensitive = 1 << 0,
        /// <summary>
        /// Normal parsing.  All switches are matched case insensitive.
        /// </summary>
        Default = 0
    }
}
