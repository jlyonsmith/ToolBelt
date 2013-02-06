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

namespace ToolBelt
{
	/// <summary>
	/// A valid command line argument 
	/// </summary>
	public class CommandLineArgument
	{
		#region Public Instance Constructors

		/// <summary>
		/// Constructs a new command line argument.
		/// </summary>
		/// <param name="attribute">The attribute that defines the argument</param>
		/// <param name="propertyInfo">The arguments property information</param>
		public CommandLineArgument(CommandLineArgumentAttribute attribute, PropertyInfo propertyInfo)
		{
			// Set these two things first
			this.attribute = attribute;
			this.propertyInfo = propertyInfo;

			if (this.IsArray)
			{
				// Get an element type if the argument is an array
				valueType = propertyInfo.PropertyType.GetElementType();
				
				if (valueType == typeof(object))
				{
					throw new NotSupportedException(CommandLineParserResources.PropertyIsNotAStronglyTypedArray(propertyInfo.Name));
				}
			}
			else if (this.IsCollection)
			{
				// Locate Add method with 1 parameter that is not System.Object
				foreach (MethodInfo method in propertyInfo.PropertyType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
				{
					if (method.Name == "Add" && method.GetParameters().Length == 1)
					{
						ParameterInfo parameter = method.GetParameters()[0];

						if (parameter.ParameterType != typeof(object))
						{
							addMethod = method;
							valueType = parameter.ParameterType;
						}
					}
					
					if (method.Name == "GetEnumerator" && method.GetParameters().Length == 0 && typeof(IEnumerator).IsAssignableFrom(method.ReturnType))
					{
						getEnumeratorMethod = method;
					}
					
					if (addMethod != null && getEnumeratorMethod != null)
						break;
				}

				// If we didn't find an appropriate Add method we can't write to the collection
				if (addMethod == null)
				{
					throw new NotSupportedException(CommandLineParserResources.PropertyHasNoAddMethodWithNonObjectParameter(propertyInfo.Name));
				}

				// The collection derives from ICollection, so it would be really surprising if it didn't implement GetEnumerator
				Debug.Assert(getEnumeratorMethod != null);
			}
			else
			{
				// The argument is not an array or collection, so it's safe to take the property type as the value type
				this.valueType = propertyInfo.PropertyType;
			}

			this.values = new ArrayList(AllowMultiple ? 4 : 1);
		}

		#endregion

		#region Public Instance Properties

		/// <summary>
		/// Gets the underlying <see cref="Type" /> of the argument.
		/// </summary>
		/// <remarks>
		/// If the <see cref="Type" /> of the argument is a collection type,
		/// this property will returns the underlying type of the collection.
		/// </remarks>
		public Type ValueType
		{
			get { return valueType; }
		}

		/// <summary>
		/// Gets the name of the argument.
		/// </summary>
		/// <value>The long name of the argument.</value>
		public string Name
		{
			get { return attribute.Name; }
		}

		/// <summary>
		/// Gets the short name of the argument.
		/// </summary>
		public string ShortName
		{
			get { return attribute.ShortName; }
		}

		/// <summary>
		/// Gets the description of the argument.
		/// </summary>
		public string Description
		{
			get { return attribute.Description; }
		}

		/// <summary>
		/// Gets the hint for the argument value.
		/// </summary>
		public string ValueHint
		{
			get { return attribute.ValueHint; }
		}

		/// <summary>
		/// Gets a value indicating whether an instance of this argument is set.
		/// </summary>
		public bool HasArgumentValue
		{
			get { return Count > 0; }
		}

		/// <summary>
		/// Gets a count of the number of instances of this argument.
		/// </summary>
		public int Count
		{
			get
			{
				if (values != null)
					return values.Count;
				else
					return 0;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the argument can be specified multiple times.
		/// </summary>
		/// <value>
		/// <see langword="true" /> if the argument may be specified multiple 
		/// times; otherwise, <see langword="false" />.
		/// </value>
		public bool AllowMultiple
		{
			get { return valueType != null && (IsCollection || IsArray); }
		}

		/// <summary>
		/// Gets a value indicating whether the argument is collection-based.
		/// </summary>
		/// <value>
		/// <see langword="true" /> if the argument is collection-based; otherwise, 
		/// <see langword="false" />.
		/// </value>
		public bool IsCollection
		{
			get { return typeof(ICollection).IsAssignableFrom(propertyInfo.PropertyType); }
		}

		/// <summary>
		/// Gets a value indicating whether the argument is array-based.
		/// </summary>
		public bool IsArray
		{
			get { return propertyInfo.PropertyType.IsArray; }
		}

		/// <summary>
		/// Gets a value indicating whether the argument is the default argument.
		/// </summary>
		public bool IsDefault
		{
			get { return attribute is DefaultCommandLineArgumentAttribute; }
		}

		/// <summary>
		/// Gets a value indicating whether the argument is the unprocessed argument.
		/// </summary>
		public bool IsUnprocessed
		{
			get { return attribute is UnprocessedCommandLineArgumentAttribute; }
		}

		/// <summary>
		/// Gets a value indicating whether the argument is the command argument.
		/// </summary>
		public bool IsCommand
		{
			get { return attribute is CommandCommandLineArgumentAttribute; }
		}

		#endregion

		#region Public Instance Methods

		/// <summary>
		/// This method pushes the value from this argument into the matching property into the target object.
		/// </summary>
		/// <param name="target">The object on which the value of the argument should be set.</param>
		public void SetTargetValue(object target)
		{
			try 
			{
				if (IsArray)
				{
					propertyInfo.SetValue(target, values.ToArray(valueType), BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
				}
				else if (IsCollection)
				{
					object value = propertyInfo.GetValue(target,
						BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
	
					// If value of property is null, create new instance of collection 
					if (value == null)
					{
						value = Activator.CreateInstance(propertyInfo.PropertyType,
							BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.InvariantCulture);
	
						propertyInfo.SetValue(target, value, BindingFlags.Default,
							null, null, CultureInfo.InvariantCulture);
					}
	
					try
					{
						// Invoke the Add method a bunch of times to add the elements
						foreach (object item in values)
						{
							addMethod.Invoke(value, BindingFlags.Default, null,
								new object[] { item }, CultureInfo.InvariantCulture);
						}
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
					if (values.Count > 0)
					{
						propertyInfo.SetValue(target, values[0],
							BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
					}
				}
			}
			catch (TargetInvocationException e)
			{
				// Rethrow this exception as the inner exception if it a command line arguement exception.  That 
				// is one way the user can check for correct command line arguments
				if (e.InnerException != null && e.InnerException is CommandLineArgumentException)
					throw e.InnerException;
					
				// Otherwise just keep on throwing
				throw;
			}
		}

		/// <summary>
		/// This method pulls the values from the matching property in the target into this argument.  Existing values are
		/// overwritten.
		/// </summary>
		/// <param name="target">The object from which the value of the argument should be set.</param>
		public void GetTargetValue(object target)
		{
			values.Clear();

			object value = propertyInfo.GetValue(target, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
			
			if (value == null)
				return;
		
			if (IsArray)
			{
				values.AddRange((ICollection)value);
			}
			else if (IsCollection)
			{
				IEnumerator enumerator = (IEnumerator)getEnumeratorMethod.Invoke(
					value, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
				
				while (enumerator.MoveNext())
				{
					values.Add(enumerator.Current);
				}
			}
			else
			{
				values.Add(value);
			}
		}

		/// <summary>
		/// Assigns the specified value to the argument.
		/// </summary>
		/// <param name="value">The value that should be assigned to the argument.</param>
		/// <exception cref="CommandLineArgumentException">
		/// <para>Duplicate argument OR invalid value.</para>
		/// </exception>
		public void SetArgumentValue(string value)
		{
			if (HasArgumentValue && !AllowMultiple)
			{
				throw new CommandLineArgumentException(CommandLineParserResources.DuplicateCommandLineArgument(Name));
			}

			// Don't do any processing if this is to be an unprocessed argument list (which by definition must 
			// be an array or a collection)
			if (IsUnprocessed)
			{
				values.Add(value);
				return;
			}
			
			// Specifically not initialized so we can see if the big if block below has any holes
			object newValue = null;

			// Null is only valid for bool variables.  Empty string is never valid
			if ((value == null && ValueType != typeof(bool)) || (value != null && value.Length == 0))
			{
				throw new CommandLineArgumentException(CommandLineParserResources.InvalidValueForCommandLineArgument(value, Name));
			}
			
			try
			{
				// If there is an initializer type/method use it 
				if (this.attribute.Initializer != null)
				{
					// Look for a public static Parse method on the initializer class
					System.Reflection.MethodInfo parseMethod = attribute.Initializer.GetMethod(
                        this.attribute.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null,
						CallingConventions.Standard, new Type[] { typeof(string) }, null);

					if (parseMethod != null)
					{
						// Call the Parse method
						newValue = parseMethod.Invoke(null, BindingFlags.Default, null,
							new object[] { value }, CultureInfo.InvariantCulture);
					}
					else
					{
						throw new CommandLineArgumentException(
							CommandLineParserResources.InvalidInitializerClassForCommandLineArgument(attribute.Initializer.ToString(), Name));
					}
				}
				else if (ValueType == typeof(string))
				{
					newValue = value;
				}
				else if (ValueType == typeof(bool))
				{
					newValue = (object)true;
				}
				else if (ValueType.IsEnum)
				{
					try
					{
						newValue = Enum.Parse(ValueType, value, true);
					}
					catch (ArgumentException ex)
					{
						string s = String.Empty;
							
						foreach (object obj in Enum.GetValues(ValueType))
						{
							s += obj.ToString() + ", ";
						}
							
						// strip last ,
						s = s.Substring(0, s.Length - 2) + ".";
							
						throw new CommandLineArgumentException(
							CommandLineParserResources.InvalidValueForCommandLineArgumentWithValid(value, Name, s), ex);
					}
				}
                else
                {
                    // Look for a public static Parse method on the type of the property
                    System.Reflection.MethodInfo parseMethod = ValueType.GetMethod(
                        "Parse", BindingFlags.Public | BindingFlags.Static, null,
                        CallingConventions.Standard, new Type[] { typeof(string) }, null);

                    if (parseMethod != null)
                    {
                        // Call the Parse method
                        newValue = parseMethod.Invoke(null, BindingFlags.Default, null,
                            new object[] { value }, CultureInfo.InvariantCulture);
                    }
                    else if (ValueType.IsClass)
                    {
                        // Search for a constructor that takes a string argument
                        ConstructorInfo stringArgumentConstructor =
                            ValueType.GetConstructor(new Type[] { typeof(string) });

                        if (stringArgumentConstructor != null)
                        {
                            newValue = stringArgumentConstructor.Invoke(BindingFlags.Default, null, new object[] { value }, CultureInfo.InvariantCulture);
                        }
                    }
                }

				if (newValue == null)
				{
                    throw new CommandLineArgumentException(CommandLineParserResources.NoWayToInitializeTypeFromString(value, Name));
                }
			}
			catch (Exception e)
			{
				if (!(e is CommandLineArgumentException))
				{
					throw new CommandLineArgumentException(CommandLineParserResources.InvalidValueForCommandLineArgument(value, Name), e);
				}
			}
				
			values.Add(newValue);
		}

		/// <summary>
		/// Returns the value of the argument as a string
		/// </summary>
		public override string ToString()
		{
			if (values.Count == 0)
			{
				return String.Empty;
			}
			else if (this.IsDefault || this.IsUnprocessed || this.IsCommand)
			{
				StringBuilder sb = new StringBuilder();
				IEnumerator enumerator = values.GetEnumerator();
				bool more = enumerator.MoveNext();

				while (more)
				{
					object value = enumerator.Current;

					more = enumerator.MoveNext();

					bool quote = (value.ToString().IndexOf(' ') != -1);

					sb.AppendFormat(CultureInfo.InvariantCulture, quote ? "\"{0}\"{1}" : "{0}{1}", value, more ? " " : "");
				}

				return sb.ToString();
			}
			else
			{
				StringBuilder sb = new StringBuilder();
				IEnumerator enumerator = values.GetEnumerator();
				bool more = enumerator.MoveNext();
				
				while (more)
				{
					object value = enumerator.Current;
					
					more = enumerator.MoveNext();

                    if (ValueType == typeof(bool))
					{
                        // Only append a boolean flag if it's true
                        if ((bool)value)
						    sb.AppendFormat(CultureInfo.InvariantCulture, "-{0}{1}", Name, more ? " " : "");
					}
					else 
					{
						bool quote = (value.ToString().IndexOf(' ') != -1);

                        sb.AppendFormat(CultureInfo.InvariantCulture, quote ? "-{0}:\"{1}\"{2}" : "-{0}:{1}{2}", Name, value, more ? " " : "");
					}
				}
				
				return sb.ToString();
			}
		}

		/// <summary>
		/// Tests if the given command is valid for this argument.  A null or empty command is valid for all arguments.
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		public bool IsValidForCommand(string command)
		{
			if (String.IsNullOrEmpty(command))
				return true;
			
			if (attribute.Commands == null)
				return false;

			string[] commands = attribute.Commands.Split(',', ';');
			
			for (int i = 0; i < commands.Length; i++)
				commands[i] = commands[i].Trim().ToLower(CultureInfo.InvariantCulture); // commands are not localized
				
			return Array.Exists<string>(commands, delegate(string item) { return String.CompareOrdinal(item, command) == 0; });
		}

		#endregion

		#region Private Instance Fields

		private ArrayList values;
		private PropertyInfo propertyInfo;
		private MethodInfo addMethod;
		private MethodInfo getEnumeratorMethod;
		private Type valueType;
		private CommandLineArgumentAttribute attribute;

		#endregion
	}

	/// <summary>
	/// Command line parser.
	/// </summary>
	public class CommandLineParser 
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandLineParser" /> class.  See <see cref="CommandLineParser(Type, Type, CommandLineParserFlags)"/> 
		/// for full description of arguments.
		/// </summary>
		/// <param name="argumentSpecificationType">The <see cref="Type" /> in which the command line arguments are defined.</param>
		/// <exception cref="ArgumentNullException"><paramref name="argumentSpecificationType" /> is a null reference.</exception>
		public CommandLineParser(Type argumentSpecificationType)
			: this(argumentSpecificationType, null, CommandLineParserFlags.Default)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandLineParser" /> class.  See <see cref="CommandLineParser(Type, Type, CommandLineParserFlags)"/> 
		/// for full description of arguments.
		/// </summary>
		/// <param name="argumentSpecificationType">The <see cref="Type" /> from which the possible command line arguments should be retrieved.</param>
		/// <param name="flags">See <see cref="CommandLineParserFlags"/>.</param>
		/// <exception cref="ArgumentNullException"><paramref name="argumentSpecificationType" /> is a null reference.</exception>
		public CommandLineParser(Type argumentSpecificationType, CommandLineParserFlags flags) : 
			this(argumentSpecificationType, null, flags) 
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandLineParser" /> class.  See <see cref="CommandLineParser(Type, Type, CommandLineParserFlags)"/> 
		/// for full description of arguments.
		/// </summary>
		/// <param name="argumentSpecificationType">The <see cref="Type" /> in which the command line arguments are defined.</param>
		/// <param name="resourceReaderType">A resource reader object with a <code>GetString</code> method</param>
		/// <exception cref="ArgumentNullException"><paramref name="argumentSpecificationType" /> is a null reference.</exception>
		public CommandLineParser(Type argumentSpecificationType, Type resourceReaderType) :
			this(argumentSpecificationType, resourceReaderType, CommandLineParserFlags.Default)
		{
		} 
		
		/// <summary>
		/// Initializes a new instance of the <see cref="CommandLineParser" /> class.  Provides a resource reader for localization of the command 
		/// line arguments.  String specified in the command line attributes are used to look up corresponding localized strings in the resources.
		/// Also provides flags to control parsing.
		/// </summary>
		/// <param name="argumentSpecificationType">The <see cref="Type" />  in which the command line arguments are defined.</param>
		/// <param name="resourceReaderType">A resource reader object with a <code>GetString</code> method</param>
		/// <param name="flags">See <see cref="CommandLineParserFlags"/>.</param>
		/// <exception cref="ArgumentNullException"><paramref name="argumentSpecificationType" /> is a null reference.</exception>
		public CommandLineParser(Type argumentSpecificationType, Type resourceReaderType, CommandLineParserFlags flags) 
		{
			if (argumentSpecificationType == null) 
			{
				throw new ArgumentNullException("argumentSpecificationType");
			}

			this.argumentSpecificationType = argumentSpecificationType;
			this.resourceReaderType = resourceReaderType;
			this.flags= flags;
			this.responseFiles = new Stack<string>();

			argumentCollection = new List<CommandLineArgument>();

			// Look ONLY for public instance properties.  NOTE: Don't change this behavior!
			foreach (PropertyInfo propertyInfo in argumentSpecificationType.GetProperties(BindingFlags.Instance | BindingFlags.Public)) 
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

				if (attribute is DefaultCommandLineArgumentAttribute) 
				{
					if (HasDefaultArgument)
						throw new ArgumentException(CommandLineParserResources.DefaultArgumentAlreadyDefined);
					
					defaultArgument = new CommandLineArgument(attribute, propertyInfo);
				}
				else if (attribute is UnprocessedCommandLineArgumentAttribute)
				{
					if (HasUnprocessedArgument)
						throw new ArgumentException(CommandLineParserResources.UnprocessedArgumentAlreadyDefined);
						
					unprocessedArgument = new CommandLineArgument(attribute, propertyInfo);
					
					if (!unprocessedArgument.AllowMultiple)
						throw new ArgumentException(CommandLineParserResources.UnprocessedArgumentMustBeArrayOrCollection);
				}
				else if (attribute is CommandCommandLineArgumentAttribute)
				{
					if (HasCommandArgument)
						throw new ArgumentException(CommandLineParserResources.CommandArgumentAlreadyDefined);
						
					commandArgument = new CommandLineArgument(attribute, propertyInfo);
				} 
				else
				{
					attribute.Description = ExternalGetString(attribute.Description);
					
					if (attribute.Description == null)
					{
						throw new ArgumentException(
							CommandLineParserResources.PropertyDoesNotHaveAValueForDescription(propertyInfo.Name));
					}
					
					// If not being case sensitive, make everything lower case.
					if (!CaseSensitive)
					{
						attribute.Name = attribute.Name.ToLower(CultureInfo.InvariantCulture);
					
						if (attribute.ShortName != null)
							attribute.ShortName = attribute.ShortName.ToLower(CultureInfo.InvariantCulture);
					}
				
					argumentCollection.Add(new CommandLineArgument(attribute, propertyInfo));
				}
			}
			
			if (HasUnprocessedArgument && !HasDefaultArgument)
			{
				throw new ArgumentException(CommandLineParserResources.UnprocessedRequiresDefaultArguments);
			}
		}
		
		#endregion

		#region Public Instance Properties

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
		/// Gets the total number of processed arguments.
		/// </summary>
		public int ArgumentCount
		{
			get 
			{
				int count = 0;
				
				foreach(CommandLineArgument argument in argumentCollection) 
				{
					if (argument.HasArgumentValue) 
					{
						count += argument.Count;
					}
				}

				if (HasDefaultArgument) 
				{
					count += defaultArgument.Count;
				}
				
				return count;
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
					object[] copyrightAttributes = argumentSpecificationType.GetCustomAttributes(typeof(CommandLineCopyrightAttribute), true);
					
					if (copyrightAttributes.Length > 0)
					{
						CommandLineCopyrightAttribute copyrightAttribute = (CommandLineCopyrightAttribute)copyrightAttributes[0];

						// This one is localizable
						copyright = ExternalGetString(copyrightAttribute.Copyright);
					}
					else
					{
						copyrightAttributes = argumentSpecificationType.Assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), true);

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
					object[] configurationAttributes = argumentSpecificationType.GetCustomAttributes(typeof(CommandLineConfigurationAttribute), true);

					if (configurationAttributes.Length > 0)
					{
						CommandLineConfigurationAttribute configurationAttribute = (CommandLineConfigurationAttribute)configurationAttributes[0];

						// This is localizable
						configuration = ExternalGetString(configurationAttribute.Configuration);
					}
					else
					{
						configurationAttributes = argumentSpecificationType.Assembly.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), true);
						
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
					Assembly assembly = argumentSpecificationType.Assembly;
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
					object[] descriptionAttributes = argumentSpecificationType.GetCustomAttributes(typeof(CommandLineDescriptionAttribute), true);

					if (descriptionAttributes.Length > 0)
					{
						CommandLineDescriptionAttribute descriptionAttribute = (CommandLineDescriptionAttribute)descriptionAttributes[0];

						// This value can be localized
						description = ExternalGetString(descriptionAttribute.Description);
					}
					else
					{
						descriptionAttributes = argumentSpecificationType.Assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), true);

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
					object[] titleAttributes = argumentSpecificationType.GetCustomAttributes(typeof(CommandLineTitleAttribute), true);

					if (titleAttributes.Length > 0)
					{
						CommandLineTitleAttribute titleAttribute = (CommandLineTitleAttribute)titleAttributes[0];

						// This value can be localized
						title = ExternalGetString(titleAttribute.Title);
					}
					else
					{
						titleAttributes = argumentSpecificationType.Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), true);

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
		/// Gets the command line arguments in cannonical form.  If there are any arguments, the string
		/// begins with a space and each argument is separated by a space, otherwise it is empty.
		/// </summary>
		public string Arguments
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				List<CommandLineArgument>.Enumerator enumerator = argumentCollection.GetEnumerator();

				if (HasCommandArgument)
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
				
				if (HasDefaultArgument && (!HasCommandArgument || defaultArgument.IsValidForCommand(commandArgument.ToString())))
				{
					string s = defaultArgument.ToString();
					
					if (s.Length > 0)
						sb.Append(" " + s);
				}

				if (HasUnprocessedArgument && (!HasCommandArgument || unprocessedArgument.IsValidForCommand(commandArgument.ToString())))
				{
					string s = unprocessedArgument.ToString();
					
					if (s.Length > 0)
						sb.Append(" " + s);
				}

				return sb.ToString();
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

		#endregion

		#region Public Instance Methods

		/// <summary>
		/// Parse command line arguments and set a target type instance with argument values.
		/// </summary>
		/// <param name="args"></param>
		/// <param name="target"></param>
		public void ParseAndSetTarget(string[] args, object target)
		{
			Parse(args);
			SetTarget(target);
		}

		/// <summary>
		/// Parses an argument list.
		/// </summary>
		/// <param name="args">The arguments to parse.</param>
		/// <exception cref="ArgumentNullException"><paramref name="target" /> is a null reference.</exception>
		/// <exception cref="ArgumentException">The <see cref="Type" /> of <paramref name="target" /> does not match the argument specification that was used to initialize the parser.</exception>
		public void Parse(IList<string> args)
		{
			if (args == null || args.Count == 0)
				return;

			foreach (string argument in args)
			{
				if (argument.Length == 0)
					continue;

				if (HasDefaultArgument && defaultArgument.HasArgumentValue && HasUnprocessedArgument)
				{
					// We have a default argument now, everything else should be left unprocessed
					unprocessedArgument.SetArgumentValue(argument);
					continue;
				}

				switch (argument[0])
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
							string path = argument.Substring(1);
							try
							{
								string rspFileName = Path.GetFullPath(path);
								using (TextReader rspFile = new StreamReader(rspFileName))
								{
									// read all the lines as one argument each
									List<string> rspArgs = new List<string>();
									string nextLine = null;
									while ((nextLine = rspFile.ReadLine()) != null)
										rspArgs.Add(nextLine);

									// recursively call parse; any exceptions will be propagated up
									responseFiles.Push(rspFileName);
									Parse(rspArgs);
									responseFiles.Pop();
								}
							}
							catch (CommandLineArgumentException)
							{
								// We want to catch argument exceptions from the block, but we don't want to catch
								// recursive CommandLineArgumentExceptions
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
						int endIndex = argument.IndexOfAny(new char[] { ':', '+', '-' }, 1);
						string argumentName = argument.Substring(1, endIndex == -1 ? argument.Length - 1 : endIndex - 1);
						string argumentValue;

						if (argumentName.Length + 1 == argument.Length)
						{
							// There's nothing left for a value
							argumentValue = null;
						}
						else if (argument.Length > 1 + argumentName.Length && argument[1 + argumentName.Length] == ':')
						{
							argumentValue = argument.Substring(argumentName.Length + 2);
						}
						else
						{
							argumentValue = argument.Substring(argumentName.Length + 1);
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
						
							arg = argumentCollection.Find(
								delegate(CommandLineArgument item)
								{
									return
										IsValidForCommand(item, command) &&
										String.CompareOrdinal(item.Name, argumentName) == 0 ||
										String.CompareOrdinal(item.ShortName, argumentName) == 0;
								});
						}
						else
						{
							arg = argumentCollection.Find(
								delegate(CommandLineArgument item)
								{
									return
										String.CompareOrdinal(item.Name, argumentName) == 0 ||
										String.CompareOrdinal(item.ShortName, argumentName) == 0;
								});
						}

						if (arg == null)
						{
							throw new CommandLineArgumentException(CommandLineParserResources.UnknownArgument(argument));
						}
						else
						{
							arg.SetArgumentValue(argumentValue);
						}
						break;
					default:
						if (HasCommandArgument && !commandArgument.HasArgumentValue)
						{
							string command = argument.ToLower(CultureInfo.InvariantCulture);
							
							if (!IsValidForCommand(command))
							{
								throw new CommandLineArgumentException(CommandLineParserResources.UnknownCommandArgument(command));
							}
							
							commandArgument.SetArgumentValue(command);
						}
						else if (HasDefaultArgument)
						{
							defaultArgument.SetArgumentValue(argument);
						}
						else
						{
							throw new CommandLineArgumentException(CommandLineParserResources.UnknownArgument(argument));
						}
						break;
				}
			}
		}
		
		/// <summary>
		/// Initialize a target type instance from parsed arguments.
		/// </summary>
		/// <param name="target">The target type to initialize</param>
		public void SetTarget(object target)
		{
			// NOTE: We typically do this two part parsing because of arrays.  If the target type is an array, there is no efficient 
			// way to keep adding entries to the array as arguments are seen.  Hence we gather them all up and blast them in at once.
			// But it also useful for MSBuild tasks where you don't have a textual command line at all, and for serializing command 
			// line arguments in a standard way.

			if (target == null)
			{
				throw new ArgumentNullException("target");
			}

			// Ensure that the target type is the same as the specification type or derived from it
			if (!argumentSpecificationType.IsAssignableFrom(target.GetType()))
			{
				throw new ArgumentException(
					CommandLineParserResources.TypeNotDerivedFromType(target.GetType().ToString(), argumentSpecificationType.GetType().ToString()));
			}

			foreach (CommandLineArgument arg in argumentCollection)
			{
				arg.SetTargetValue(target);
			}

			// These arguments are not part of the main collection
			if (HasCommandArgument)
			{
				commandArgument.SetTargetValue(target);
			}

			if (HasDefaultArgument)
			{
				defaultArgument.SetTargetValue(target);
			}

			if (HasUnprocessedArgument)
			{
				unprocessedArgument.SetTargetValue(target);
			}
		}

		/// <summary>
		/// Initialize parsed arguments from a target type instance.
		/// </summary>
		/// <param name="target">The target type to initialize</param>
		public void GetTargetArguments(object target)
		{
			if (target == null)
			{
				throw new ArgumentNullException("target");
			}

			// Ensure that the target type is the same as the specification type or derived from it
			if (!argumentSpecificationType.IsAssignableFrom(target.GetType()))
			{
				throw new ArgumentException(
					CommandLineParserResources.TypeNotDerivedFromType(target.GetType().ToString(), argumentSpecificationType.GetType().ToString()));
			}

			foreach (CommandLineArgument arg in argumentCollection)
			{
				arg.GetTargetValue(target);
			}

			// These three arguments are not part of the main collection
			if (HasCommandArgument)
			{
				commandArgument.GetTargetValue(target);
			}

			if (HasDefaultArgument)
			{
				defaultArgument.GetTargetValue(target);
			}

			if (HasUnprocessedArgument)
			{
				unprocessedArgument.GetTargetValue(target);
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

			if (HasCommandArgument && !IsValidForCommand(command))
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

		#endregion

		#region Private Instance Methods
		
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
	
				if (HasDefaultArgument && IsValidForCommand(defaultArgument, command))
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
						if (defaultArgument.AllowMultiple)
						{
							// Add additional default arguments
							helpText.Append(" [");
							helpText.Append(hint);
							helpText.Append(" ...]");
						}
					}
					else if (IsValidForCommand(unprocessedArgument, command))
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

			object[] attributes = this.argumentSpecificationType.GetCustomAttributes(typeof(CommandLineCommandDescriptionAttribute), true);
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
				new List<KeyValuePair<CommandLineArgument, string>>(argumentCollection.Count);

			foreach (CommandLineArgument argument in argumentCollection)
			{
				if (!IsValidForCommand(argument, command))
					continue;
				
				string valueText = "";

				if (argument.ValueHint != null)
				{
					if (argument.ValueHint != String.Empty)
						valueText = ":" + argument.ValueHint;
				}
				else if (argument.ValueType == typeof(string))
				{
					valueText = ":<" + CommandLineParserResources.Text_LowerCase + ">";
				}
				else if (
					argument.ValueType == typeof(FileInfo) ||
					argument.ValueType == typeof(ParsedPath))
				{
					valueText = ":<" + CommandLineParserResources.FileName_Lowercase + ">";
				}
				else if (
					argument.ValueType == typeof(int) ||
					argument.ValueType == typeof(uint) ||
					argument.ValueType == typeof(short) ||
					argument.ValueType == typeof(ushort))
				{
					valueText = ":<" + CommandLineParserResources.Number_Lowercase + ">";
				}
				else if (argument.ValueType != typeof(bool))
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

				string[] descriptionLines = StringUtility.WordWrap(description, (lineLength - 1) - 22);

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
					object[] attributes = this.argumentSpecificationType.GetCustomAttributes(typeof(CommandLineCommandDescriptionAttribute), true);

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
				object[] attributes = this.argumentSpecificationType.GetCustomAttributes(typeof(CommandLineDescriptionAttribute), true);

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
			object[] attributes = this.argumentSpecificationType.GetCustomAttributes(typeof(CommandLineExampleAttribute), true);

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
				return argumentCollection.Count > 0;
			}
			else
			{
				foreach (CommandLineArgument argument in argumentCollection)
				{
					if (IsValidForCommand(argument, command))
						return true;
				}

				return false;
			}
		}

		private string GetDefaultArgumentValueHint(string command)
		{
			Debug.Assert(command != null);

			object[] attributes = this.argumentSpecificationType.GetCustomAttributes(typeof(CommandLineCommandDescriptionAttribute), true);

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

			object[] attributes = this.argumentSpecificationType.GetCustomAttributes(typeof(CommandLineCommandDescriptionAttribute), true);

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
		/// Is this command a valid command.
		/// </summary>
		/// Check that a command description exists for this command.
		/// <param name="command"></param>
		/// <returns></returns>
		private bool IsValidForCommand(string command)
		{
			if (HasCommandArgument && commandArgument.IsValidForCommand(command))
				return true;

			EnsureCommandSwitches();

			return commandSwitches.ContainsKey(command);
		}

		/// <summary>
		/// Is the given command a valid command for this switch (i.e. is this switch used by this command)
		/// </summary>
		/// <param name="argument"></param>
		/// <param name="command"></param>
		/// <returns></returns>
		private bool IsValidForCommand(CommandLineArgument argument, string command)
		{
			if (argument.IsValidForCommand(command))
				return true;

			// check the command attributes instead if they contain this switch name
			EnsureCommandSwitches();

			return Array.Exists(commandSwitches[command],
				delegate(string s) { return string.CompareOrdinal(s, argument.Name) == 0; });
		}

		private void EnsureCommandSwitches()
		{
			if (commandSwitches == null)
			{
				object[] attributes = this.argumentSpecificationType.GetCustomAttributes(typeof(CommandLineCommandDescriptionAttribute), true);
				commandSwitches = new Dictionary<string, string[]>(attributes.Length);

				// Build a list of all the commands
				foreach (CommandLineCommandDescriptionAttribute attribute in attributes)
				{
					string[] switches = attribute.Switches != null ? attribute.Switches.Split(',') : new string[0];
					commandSwitches.Add(attribute.Command, switches);
				}
			}
		}


		#endregion

		#region Private Instance Fields

		private List<CommandLineArgument> argumentCollection; 
		private CommandLineArgument defaultArgument;
		private CommandLineArgument unprocessedArgument;
		private CommandLineArgument commandArgument;
		private Type argumentSpecificationType;
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
		private Dictionary<string, string[]> commandSwitches; // map of command -> switches it supports

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
		public DefaultCommandLineArgumentAttribute(string name) : base(name)
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
		public UnprocessedCommandLineArgumentAttribute(string name) : base(name)
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
		public CommandCommandLineArgumentAttribute(string name)
			: base(name)
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
		CaseSensitive = 1<<0,
		/// <summary>
		/// Normal parsing.  All switches are matched case insensitive.
		/// </summary>
		Default = 0
	}
}
