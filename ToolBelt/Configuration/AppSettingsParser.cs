using System;
using System.Runtime.Serialization;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using System.Collections;
using System.Linq;
using System.Collections.Specialized;

namespace ToolBelt
{
    public class AppSettingsArgument
    {
        #region Private Fields

        private object argTarget;
        private Type argType;
        private AppSettingsArgumentAttribute argAttribute;
        private PropertyInfo argPropertyInfo;
        private MethodInfo argAddMethod;

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
        /// Gets the <see cref="AppSettingsArgument"/> name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get
            {
                return argPropertyInfo.Name;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the setting can contain a list of multiple values
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the argument may be specified multiple 
        /// times; otherwise, <see langword="false" />.
        /// </value>
        public bool AllowsMultiple
        {
            get 
            { 
                return typeof(ICollection).IsAssignableFrom(argPropertyInfo.PropertyType); 
            }
        }

        /// <summary>
        /// Gets a value indicating whether an instance of this argument is set.  A <see cref="ValueType"/> argument
        /// is always considered not set. 
        /// </summary>
        public bool HasValue
        {
            get 
            { 
                if (AllowsMultiple && ValueCollection != null && ValueCollection.Count > 0)
                    return true;
                else if (!ArgType.IsValueType && Value != null)
                    return true;
                else
                    return false;
            }
        }

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolBelt.AppSettingsArgument"/> class.
        /// </summary>
        /// <param name="attribute">Attribute.</param>
        /// <param name="propertyInfo">Property info.</param>
        public AppSettingsArgument(object argTarget, AppSettingsArgumentAttribute attribute, PropertyInfo propertyInfo)
        {
            this.argTarget = argTarget;
            this.argPropertyInfo = propertyInfo;
            this.argType = propertyInfo.PropertyType;
            this.argAttribute = attribute;

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
                    throw new NotSupportedException(AppSettingsParserResources.PropertyHasNoAddMethodWithNonObjectParameter(argPropertyInfo.Name));
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Assigns the specified value to the argument.
        /// </summary>
        /// <param name="argString">The value that should be assigned to the argument.</param>
        /// <exception cref="AppSettingsArgumentException">
        /// <para>Duplicate argument OR invalid value.</para>
        /// </exception>
        public void ParseAndSetTarget(string argString)
        {
            // Specifically not initialized so we can see if the big if block below has any holes
            object value = null;

            // Null is only valid for bool variables; empty string is valid for nothing else
            if ((argString == null && ArgType != typeof(bool)) || (argString != null && argString.Length == 0))
            {
                throw new AppSettingsArgumentException(AppSettingsParserResources.InvalidValueForAppSettingArgument(argString, Name));
            }

            try
            {
                // If there is an initializer type/method use it over anything else
                if (this.argAttribute.Initializer != null)
                {
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
                        throw new AppSettingsArgumentException(
                            AppSettingsParserResources.InvalidInitializerClassForAppSettingArgument(argAttribute.Initializer.ToString(), Name));
                    }
                }
                else if (ArgType == typeof(string))
                {
                    value = argString;
                }
                else if (ArgType == typeof(bool))
                {
                    value = Boolean.Parse(argString);
                }
                else if (ArgType.IsEnum)
                {
                    try
                    {
                        value = Enum.Parse(ArgType, argString, true);
                    }
                    catch (ArgumentException ex)
                    {
                        string s = String.Empty;

                        foreach (object obj in Enum.GetValues(ArgType))
                        {
                            s += obj.ToString() + ", ";
                        }

                        // strip last ,
                        s = s.Substring(0, s.Length - 2) + ".";

                        throw new AppSettingsArgumentException(
                            AppSettingsParserResources.InvalidValueForAppSettingArgumentWithValid(argString, Name, s), ex);
                    }
                }
                else if (ArgType.IsGenericType && ArgType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                {
                    // Check if this is a Nullable type, and get the underlying type if it is
                    Type underlyingType = Nullable.GetUnderlyingType(ArgType);

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
                    System.Reflection.MethodInfo parseMethod = ArgType.GetMethod(
                        "Parse", BindingFlags.Public | BindingFlags.Static, null,
                        CallingConventions.Standard, new Type[] { typeof(string) }, null);

                    if (parseMethod != null)
                    {
                        // Call the Parse method
                        value = parseMethod.Invoke(null, BindingFlags.Default, null,
                            new object[] { argString }, CultureInfo.InvariantCulture);
                    }
                    else if (ArgType.IsClass)
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
                    throw new AppSettingsArgumentException(AppSettingsParserResources.NoWayToInitializeTypeFromString(ArgType.Name, Name));
                }
            }
            catch (Exception e)
            {
                if (!(e is AppSettingsArgumentException))
                {
                    throw new AppSettingsArgumentException(AppSettingsParserResources.InvalidValueForAppSettingArgument(argString, Name), e);
                }
                else
                    throw;
            }

            try
            {
                if (AllowsMultiple)
                {
                    var collection = ValueCollection;

                    // If value of the property is null, create new instance of the collection 
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
                        if (ex.InnerException is AppSettingsArgumentException)
                            throw ex.InnerException;
                        else
                            throw new NotSupportedException(AppSettingsParserResources.ProblemInvokingAddMethod(Name), ex);
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
                if (e.InnerException != null && e.InnerException is AppSettingsArgumentException)
                    throw e.InnerException;

                // Otherwise just keep on throwing up
                throw;
            }
        }

        #endregion
    }

    /// <summary>
    /// Parser for the appSettings section of the app.config file.  Automatically validates and assigns settings to strongly typed target class.
    /// </summary>
    public class AppSettingsParser
    {
        #region Fields

        Type argTargetType;
        Type resourceReaderType;
        List<AppSettingsArgument> argCollection;
        AppSettingsParserFlags flags;

        #endregion

        #region Properties
        /// <summary>
        /// Gets a value indicating if command line processing is case sensitive
        /// </summary>
        public bool CaseSensitive
        {
            get
            {
                return (flags & AppSettingsParserFlags.CaseSensitive) != 0;
            }
        }

        /// <summary>
        /// Gets the argument extracted from the target object.
        /// </summary>
        /// <value>The argument collection.</value>
        public IEnumerable<AppSettingsArgument> ArgumentCollection
        {
            get 
            {
                return argCollection;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettingsParser" /> class.  Provides a resource reader for localization of the command 
        /// line arguments.  String specified in the command line attributes are used to look up corresponding localized strings in the resources.
        /// Also provides flags to control parsing.
        /// </summary>
        /// <param name="target">The target <see cref="object" /> into which the appSettings are to be placed.</param>
        /// <param name="resourceReaderType">A resource reader object with a <code>GetString</code> method</param>
        /// <param name="flags">See <see cref="AppSettingsParserFlags"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="argumentSpecificationType" /> is a null reference.</exception>
        public AppSettingsParser(object argTarget, Type resourceReaderType = null, AppSettingsParserFlags flags = AppSettingsParserFlags.Default)
        {
            if (argTarget == null)
            {
                throw new ArgumentNullException("argTarget");
            }

            this.argTargetType = argTarget.GetType();
            this.resourceReaderType = resourceReaderType;
            this.flags = flags;

            argCollection = new List<AppSettingsArgument>();

            // Look ONLY for public instance properties.  NOTE: Don't change this behavior!
            foreach (PropertyInfo propertyInfo in argTargetType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                object[] attributes = propertyInfo.GetCustomAttributes(typeof(AppSettingsArgumentAttribute), true);
                    AppSettingsArgumentAttribute attribute;

                if (attributes.Length == 1)
                    attribute = (AppSettingsArgumentAttribute)attributes[0];
                else
                    continue;

                // Ensure that the property is readable and writeable
                if (!(propertyInfo.CanWrite && propertyInfo.CanRead))
                    throw new ArgumentException(
                        AppSettingsParserResources.PropertyShouldBeReadableAndWriteable(propertyInfo.Name));

                attribute.ValueHint = ExternalGetString(attribute.ValueHint);
                attribute.Description = ExternalGetString(attribute.Description);

                argCollection.Add(new AppSettingsArgument(argTarget, attribute, propertyInfo));
            }
        }

        #endregion

        #region Methods
        /// <summary>
        /// Parses the supplied <see cref="NameValueCollection"/> and sets the corresponding target properties
        /// </summary>
        /// <param name="settings">Settings.</param>
        public void ParseAndSetTarget(NameValueCollection settings)
        {
            if (settings == null)
                return;

            for (int i = 0; i < settings.Count; i++)
            {
                string key = settings.GetKey(i);
                string value = settings.Get(i);

                foreach (var arg in this.argCollection)
                {
                    if (String.Compare(arg.Name, key, !CaseSensitive) == 0)
                    {
                        if (arg.AllowsMultiple)
                        {
                            var parts = value.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (var part in parts)
                            {
                                arg.ParseAndSetTarget(part);
                            }
                        }
                        else
                            arg.ParseAndSetTarget(value);
                    }
                }
            }
        }

        #endregion

        #region Private Methods

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

        #endregion
    }

    /// <summary>
    /// The exception that is thrown when a value in the appSettings is not valid is not valid.
    /// </summary>
    [Serializable]
    public sealed class AppSettingsArgumentException : ArgumentException
    {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettingsArgumentException" /> class.
        /// </summary>
        public AppSettingsArgumentException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettingsArgumentException" /> class
        /// with a descriptive message.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        public AppSettingsArgumentException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettingsArgumentException" /> class
        /// with a descriptive message and an inner exception.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        /// <param name="innerException">A nested exception that is the cause of the current exception.</param>
        public AppSettingsArgumentException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private AppSettingsArgumentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }

    /// <summary>
    /// Defines a property as an command line argument 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AppSettingsArgumentAttribute : Attribute
    {
        #region Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettingsArgumentAttribute" /> class
        /// with the specified argument type.
        /// </summary>
        /// <param name="name">Specifies the checking to be done on the argument.</param>
        public AppSettingsArgumentAttribute()
        {
            this.MethodName = "Parse"; // Default
        }

        #endregion

        #region Instance Properties

        /// <summary>
        /// Gets or sets the description of the setting.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets hint text to describe the settings value. 
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

        #endregion
    }

    /// <summary>
    /// Flags that control appSettings parsing
    /// </summary>
    [Flags]
    public enum AppSettingsParserFlags
    {
        /// <summary>
        /// All settings are matched case sensitively.
        /// </summary>
        CaseSensitive = 1 << 0,
        /// <summary>
        /// Normal parsing.  All settings are matched case insensitively.
        /// </summary>
        Default = 0
    }

}

