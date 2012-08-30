using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Resources;

namespace Buckle
{
	public class BuckleTool
	{
		#region Classes
		private class ResourceItem
		{
			#region Fields
			private Type dataType;
			private string name;
			private string valueString;

			#endregion

			#region Constructors
			public ResourceItem(string name, string stringResourceValue)
				: this(name, typeof(string))
			{
				this.valueString = stringResourceValue;
			}

			public ResourceItem(string name, Type dataType)
			{
				this.name = name;
				this.dataType = dataType;
			}

			#endregion

			#region Properties
			public Type DataType
			{
				get
				{
					return this.dataType;
				}
			}

			public string Name
			{
				get
				{
					return this.name;
				}
			}

			public string ValueString
			{
				get
				{
					return this.valueString;
				}
			}
			#endregion
		}

		#endregion

		#region Fields
		private List<ResourceItem> resources = new List<ResourceItem>();
		private StreamWriter writer;
		private const string InvalidCharacters = ".$*{}|<>";
        private bool haveDrawingResources = false;
		public string ResXFileName;
		public string ResourcesFileName;
		public string CsFileName;
		public string Namespace;
		public string BaseName;
		public string WrapperClass;
		public string Modifier;
		public bool NoLogo;
		public bool ShowUsage;
		public bool Incremental;
		public bool HasOutputErrors { get; set; }
		
		#endregion

		#region Constructors
		public BuckleTool()
		{
		}

		#endregion		
	
		#region Methods
		public void Execute()
		{
            if (!NoLogo)
            {
				string version = ((AssemblyFileVersionAttribute)Assembly.GetExecutingAssembly()
					.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true)[0]).Version;
				
				WriteMessage("Buckle ResX to C# String Wrapper Class Generator. Version {0}", version);
				WriteMessage("Copyright (c) 2012, John Lyon-Smith." + Environment.NewLine);
            }
		
			if (ShowUsage)
			{
				WriteMessage(@"Generates strongly typed wrappers for string and bitmap .resx resources
	
Usage: mono Buckle.exe <args>

Arguments:
          <resx-file>              Input .resx file.
          [-o:<output-cs>]         Output .cs file.
          [-r:<output-resources>]  Output .resources file.
          [-n:<namespace>]         Namespace to use in generated C#.
          [-b:<basename>]          The root name of the resource file without its extension 
                                   but including any fully qualified namespace name. See
                                   ResourceManager constructor documentation for details.
          [-w:<wrapper-class>]     String wrapper class. See Message.cs for details.
          [-a:<access>]            Access modifier for properties and methods.
          [-q]                     Suppress logo.
          [-i]                     Incremental build. Create outputs only if out-of-date.
          [-h] or [-?]             Show help.
");
				return;
			}

			if (ResXFileName == null)
			{
				WriteError("A .resx file must be specified");
				return;
			}
			
			if (WrapperClass == null)
			{
				WriteError("A string wrapper class must be specified");
				return;
			} 
			
			if (CsFileName == null)
			{
				CsFileName = Path.ChangeExtension(ResXFileName, ".cs");
			}

			if (ResourcesFileName == null)
			{
				ResourcesFileName = Path.ChangeExtension(ResXFileName, ".resources");
			}

			if (String.IsNullOrEmpty(Modifier))
			{
				Modifier = "public";
			}

			DateTime resxLastWriteTime = File.GetLastWriteTime(ResXFileName);

			if (Incremental &&
			    resxLastWriteTime < File.GetLastWriteTime(CsFileName) &&
				resxLastWriteTime < File.GetLastWriteTime(ResourcesFileName))
			{
				return;
			}

			ReadResources();

			using (this.writer = new StreamWriter(CsFileName, false, Encoding.ASCII))
			{
				WriteCsFile();
			}

			WriteResourcesFile();
		}

		private void WriteResourcesFile()
		{
			IResourceWriter resourceWriter = new ResourceWriter(ResourcesFileName);

			foreach (var resource in resources)
			{
				resourceWriter.AddResource(resource.Name, resource.ValueString);
			}

			resourceWriter.Close();
		}

		private void WriteCsFile()
		{
			WriteNamespaceStart();
			WriteClassStart();

			int num = 0;

			foreach (ResourceItem item in resources)
			{
				if (item.DataType.IsPublic)
				{
					num++;
					this.WriteResourceMethod(item);
				}
				else
				{
					WriteWarning("Resource skipped. Type {0} is not public.", item.DataType);
				}
			}
			WriteMessage("Generated strongly typed resource wrapper method(s) for {0} resource(s) in {1}", num, ResXFileName);
			WriteClassEnd();
			WriteNamespaceEnd();
		}

		private void WriteNamespaceStart()
		{
			DateTime now = DateTime.Now;
			writer.Write(String.Format(CultureInfo.InvariantCulture, 
@"//
// This file genenerated by the Buckle tool on {0} at {1}. 
//
// Contains strongly typed wrappers for resources in {2}
//

"
				, now.ToShortDateString(), now.ToShortTimeString(), Path.GetFileName(ResXFileName)));
			
			if ((Namespace != null) && (Namespace.Length != 0))
			{
				writer.Write(String.Format(CultureInfo.InvariantCulture, 
@"namespace {0} {{
"
					, Namespace));
			}
			writer.Write(
@"using System;
using System.Reflection;
using System.Resources;
using System.Diagnostics;
using System.Globalization;
"
				);

            if (haveDrawingResources)
            {
                writer.Write(
    @"using System.Drawing;
"
                    );
            }

            writer.WriteLine();
		}

		private void WriteNamespaceEnd()
		{
			if ((Namespace != null) && (Namespace.Length != 0))
			{
				writer.Write(
@"}
"
					);
			}
		}

		private void WriteClassStart()
		{
			writer.Write(string.Format(CultureInfo.InvariantCulture, 
@"
/// <summary>
/// Strongly typed resource wrappers generated from {0}.
/// </summary>
{1} class {2}
{{
    internal static readonly ResourceManager ResourceManager = new ResourceManager("
                ,
                Path.GetFileName(ResXFileName), 
                Modifier, 
                Path.GetFileNameWithoutExtension(CsFileName)));

            if (String.IsNullOrWhiteSpace(this.BaseName))
            {
                writer.Write(string.Format(CultureInfo.InvariantCulture,
@"typeof({0}));
"
				    , 
                    Path.GetFileNameWithoutExtension(CsFileName)));
            }
            else
            {
                writer.Write(string.Format(CultureInfo.InvariantCulture,
@"""{0}"", Assembly.GetExecutingAssembly());
"
				, 
                this.BaseName));
            }
		}

		private void WriteClassEnd()
		{
			writer.Write(
@"}
");
		}

		private void WriteResourceMethod(ResourceItem item)
		{
			StringBuilder builder = new StringBuilder(item.Name);
			for (int i = 0; i < item.Name.Length; i++)
			{
				if (InvalidCharacters.IndexOf(builder[i]) != -1)
				{
					builder[i] = '_';
				}
			}
			if (item.DataType == typeof(string))
			{
				string parametersWithTypes = string.Empty;
				string parameters = string.Empty;
				int paramCount = 0;
				try
				{
					paramCount = this.GetNumberOfParametersForStringResource(item.ValueString);
				}
				catch (ApplicationException exception)
				{
					WriteError("Resource has been skipped: {0}", exception.Message);
				}
				for (int j = 0; j < paramCount; j++)
				{
					string str3 = string.Empty;
					if (j > 0)
					{
						str3 = ", ";
					}
					parametersWithTypes = parametersWithTypes + str3 + "object param" + j.ToString(CultureInfo.InvariantCulture);
					parameters = parameters + str3 + "param" + j.ToString(CultureInfo.InvariantCulture);
				}
				
				if ((item.ValueString != null) && (item.ValueString.Length != 0))
				{
					writer.Write(
@"
    /// <summary>"
						);
					foreach (string str4 in item.ValueString.Replace("\r", string.Empty).Split("\n".ToCharArray()))
					{
						writer.Write(string.Format(CultureInfo.InvariantCulture, 
@"
    /// {0}"
							, str4));
					}
					writer.Write(
@"
    /// </summary>"
						);
				}

				if ((string.Equals(WrapperClass, "String", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(WrapperClass, "System.String", StringComparison.Ordinal)))
				{
					WriteStringMethodBody(item, Path.GetFileNameWithoutExtension(CsFileName), 
						builder.ToString(), paramCount, parameters, parametersWithTypes);
				}
				else
				{
					WriteMessageMethodBody(item, Path.GetFileNameWithoutExtension(CsFileName), 
                    	builder.ToString(), paramCount, parameters, parametersWithTypes);
				}
			}
			else
			{
				writer.Write(string.Format(CultureInfo.InvariantCulture, 
@"
    public static {0} {1}
    {{
        get
        {{
            return ({0})ResourceManager.GetObject(""{2}"");
        }}
    }}"
					, item.DataType, builder, item.Name ));
			}
		}

		private void WriteStringMethodBody(
			ResourceItem item, 
			string resourceClassName, 
			string methodName, 
			int paramCount, 
			string parameters, 
			string parametersWithTypes)
		{
			string str = "temp";
			if (parameters.Length > 0)
			{
				str = "string.Format(CultureInfo.CurrentCulture, temp, " + parameters + ")";
			}
			if (paramCount == 0)
			{
				writer.Write(string.Format(CultureInfo.InvariantCulture, 
@"
    public static {0} {1}
    {{
        get
        {{
            string temp = ResourceManager.GetString(""{2}"", CultureInfo.CurrentUICulture);
            return {3};
        }}
    }}
"
					, WrapperClass, methodName, item.Name, str ));
			}
			else
			{
				writer.Write(string.Format(CultureInfo.InvariantCulture, 
@"
    public static {0} {1}({2})
    {{
        string temp = ResourceManager.GetString(""{3}"", CultureInfo.CurrentUICulture);
        return {4};
    }}
"
					, WrapperClass, methodName, parametersWithTypes, item.Name, str ));
			}
		}

		private void WriteMessageMethodBody(
			ResourceItem item, 
			string resourceClassName, 
			string methodName, 
			int paramCount, 
			string parameters, 
			string parametersWithTypes)
		{
			if (paramCount == 0)
			{
				writer.Write(string.Format(CultureInfo.InvariantCulture, 
@"
    public static {0} {1}
    {{
        get
        {{
            return new {0}(""{2}"", typeof({3}), ResourceManager, null);
        }}
    }}
"
					, WrapperClass, methodName, item.Name, resourceClassName ));
			}
			else
			{
				writer.Write(string.Format(CultureInfo.InvariantCulture, 
@"
    public static {0} {1}({2})
    {{
        Object[] o = {{ {4} }};
        return new {0}(""{3}"", typeof({5}), ResourceManager, o);
    }}
"
					, WrapperClass, methodName, parametersWithTypes, item.Name, parameters, resourceClassName ));
			}
		}

		public int GetNumberOfParametersForStringResource(string resourceValue)
		{
			string input = resourceValue.Replace("{{", "").Replace("}}", "");
			string pattern = "{(?<value>[^{}]*)}";
			int num = -1;
			for (Match match = Regex.Match(input, pattern); match.Success; match = match.NextMatch())
			{
				string str3 = null;
				try
				{
					str3 = match.Groups["value"].Value;
				}
				catch
				{
				}
				if ((str3 == null) || (str3.Length == 0))
				{
					throw new ApplicationException(
						string.Format(CultureInfo.InvariantCulture, "\"{0}\": Empty format string at position {1}", new object[] { input, match.Index }));
				}
				string s = str3;
				int length = str3.IndexOfAny(",:".ToCharArray());
				if (length > 0)
				{
					s = str3.Substring(0, length);
				}
				int num3 = -1;
				try
				{
					num3 = (int)uint.Parse(s, CultureInfo.InvariantCulture);
				}
				catch (Exception exception)
				{
					throw new ApplicationException(string.Format(CultureInfo.InvariantCulture, "\"{0}\": {1}: {{{2}}}", new object[] { input, exception.Message, str3 }), exception);
				}
				if (num3 > num)
				{
					num = num3;
				}
			}
			return (num + 1);
		}

		private void ReadResources()
		{
			this.resources.Clear();
			XmlDocument document = new XmlDocument();
			
            document.Load(this.ResXFileName);

            Dictionary<string, string> assemblyDict = new Dictionary<string, string>();

            foreach (XmlElement element in document.DocumentElement.SelectNodes("assembly"))
            {
                assemblyDict.Add(element.GetAttribute("alias"), element.GetAttribute("name"));
            }

			foreach (XmlElement element in document.DocumentElement.SelectNodes("data"))
			{
				string attribute = element.GetAttribute("name");
				if ((attribute == null) || (attribute.Length == 0))
				{
					WriteWarning("Resource skipped. Empty name attribute: {0}", element.OuterXml);
					continue;
				}
				
                Type dataType = null;
				string typeName = element.GetAttribute("type");

                if ((typeName != null) && (typeName.Length != 0))
				{
                    string[] parts = typeName.Split(',');

                    // Replace assembly alias with full name
                    typeName = parts[0] + ", " + assemblyDict[parts[1].Trim()];

					try
					{
						dataType = Type.GetType(typeName, true);
					}
					catch (Exception exception)
					{
						WriteWarning("Resource skipped. Could not load type {0}: {1}", typeName, exception.Message);
						continue;
					}
				}
				
                ResourceItem item = null;
				
                // String resources typically have no type name
                if ((dataType == null) || (dataType == typeof(string)))
				{
					string stringResourceValue = null;
					XmlNode node = element.SelectSingleNode("value");
					if (node != null)
					{
						stringResourceValue = node.InnerXml;
					}
					if (stringResourceValue == null)
					{
						WriteWarning("Resource skipped.  Empty value attribute: {0}", element.OuterXml);
						continue;
					}
					item = new ResourceItem(attribute, stringResourceValue);
				}
                else
                {
                    if (dataType == typeof(Icon) || dataType == typeof(Bitmap))
                        haveDrawingResources = true;

                    item = new ResourceItem(attribute, dataType);
                }
				this.resources.Add(item);
			}
		}

		public void ProcessCommandLine(string[] args)
		{
			foreach (var arg in args)
			{
				if (arg.StartsWith("-"))
				{
					switch (arg[1])
					{
					case 'h':
					case '?':
						ShowUsage = true;
						return;
					case 'i':
						Incremental = true;
						continue;
					case 'q':
						NoLogo = true;
						continue;
					case 'b':
						CheckAndSetArgument(arg, ref BaseName);
						break;
					case 'o':
						CheckAndSetArgument(arg, ref CsFileName); 
						break;
					case 'r':
						CheckAndSetArgument(arg, ref ResourcesFileName); 
						break;
					case 'n':
						CheckAndSetArgument(arg, ref Namespace); 
						break;
					case 'w':
						CheckAndSetArgument(arg, ref WrapperClass); 
						if (WrapperClass != "Message" && WrapperClass != "String")
							throw new ApplicationException(string.Format("Wrapper class must be Message or String"));
						break;
					case 'm':
						CheckAndSetArgument(arg, ref Modifier); 
						if (Modifier != "public" && Modifier != "internal")
							throw new ApplicationException(string.Format("Wrapper class must be public or internal"));
						break;
					default:
						throw new ApplicationException(string.Format("Unknown argument '{0}'", arg[1]));
					}
				}
				else if (String.IsNullOrEmpty(ResXFileName))
				{
					ResXFileName = arg;
				}
				else
				{
					throw new ApplicationException("Only one .resx file can be specified");
				}
			}
		}

		private void CheckAndSetArgument(string arg, ref string val)
		{
			if (arg[2] != ':')
			{
				throw new ApplicationException(string.Format("Argument {0} is missing a colon", arg[1]));
			}
	
			if (string.IsNullOrEmpty(val))
		    {
				val = arg.Substring(3);
			}
			else
			{
				throw new ApplicationException(string.Format("Argument {0} has already been set", arg[1]));
			}
		}

		private void WriteError(string format, params object[] args)
		{
			Console.Write("error: ");
			Console.WriteLine(format, args);
			this.HasOutputErrors = true;
		}

		private void WriteWarning(string format, params object[] args)
		{
			Console.Write("warning: ");
			Console.WriteLine(format, args);
			this.HasOutputErrors = true;
		}

		private void WriteMessage(string format, params object[] args)
		{
			Console.WriteLine(format, args);
		}

		#endregion
	}
}
