using System;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;

namespace Chalk
{
	public class ChalkTool
	{
		private string filesAtom;
		public bool HasOutputErrors { get; set; }
		public bool ShowUsage { get; set; }
		public bool NoLogo { get; set; }

		public ChalkTool()
		{
		}

		public void Execute()
		{
			if (!NoLogo)
			{
				string version = ((AssemblyFileVersionAttribute)Assembly.GetExecutingAssembly()
				                  .GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true)[0]).Version;
				
				WriteMessage("Chalk Version Number Maintainer. Version {0}", version);
				WriteMessage("Copyright (c) 2012, John Lyon-Smith." + Environment.NewLine);
			}
			
			if (ShowUsage)
			{
				WriteMessage(@"Creates and increments version information in .version file in .sln directory.
	
Usage: mono Chalk.exe ...

Arguments:
          [-q]                    Suppress logo
          [-h] or [-?]            Show help
");
				return;
			}
			
			string projectSln = GetProjectSolution();
			
			if (projectSln == null)
			{
				WriteMessage("Cannot find .sln file to determine project root.");
			}
			
			WriteMessage("Project root is '{0}'", Path.GetDirectoryName(projectSln));
			
			string projectFileName = Path.GetFileName(projectSln);
			string projectName = projectFileName.Substring(0, projectFileName.IndexOf('.'));
			string versionFile = Path.Combine(Path.GetDirectoryName(projectSln), projectName + ".version");
			
			WriteMessage("Version file is '{0}'", versionFile);
			
			int major;
			int minor;
			int build;
			int revision;
			int startYear;
			string[] fileList;
			
			if (File.Exists(versionFile))
				ReadVersionFile(versionFile, out fileList, out major, out minor, out build, out revision, out startYear);
			else
			{
				major = 1;
				minor = 0;
				build = 0;
				revision = 0;
				startYear = DateTime.Now.Year;
				fileList = new string[] { };
			}
			
			int jBuild = JDate(startYear);
			
			if (build != jBuild)
			{
				revision = 0;
				build = jBuild;
			}
			else 
			{
				revision++;
			}
			
			string versionBuildAndRevision = String.Format("{0}.{1}", build, revision);
			string versionMajorAndMinor = String.Format("{0}.{1}", major, minor);
			string versionMajorMinorAndBuild = String.Format("{0}.{1}.{2}", major, minor, build);
			string versionFull = String.Format("{0}.{1}.{2}.{3}", major, minor, build, revision);
			string versionFullCsv = versionFull.Replace('.', ',');
			
			WriteMessage("New version is {0}", versionFull);
			WriteMessage("Updating version information in files:");
			
			foreach (string file in fileList)
			{
				string path = Path.Combine(Path.GetDirectoryName(projectSln), file);
				
				if (!File.Exists(path))
				{
					WriteMessage("File '{0}' does not exist", path);
					break;
				}
				
				switch (Path.GetExtension(path))
				{
				case ".cs":
					UpdateCSVersion(path, versionMajorAndMinor, versionFull);
					break;
					
				case ".rc":
					UpdateRCVersion(path, versionFull, versionFullCsv);
					break;
					
				case ".wxi":
					UpdateWxiVersion(path, versionMajorAndMinor, versionBuildAndRevision);
					break;
					
				case ".wixproj":
				case ".proj":
					UpdateProjVersion(path, versionFull, projectName);
					break;
					
				case ".vsixmanifest":
					UpdateVsixManifestVersion(path, versionFull);
					break;
					
				case ".config":
					UpdateConfigVersion(path, versionMajorAndMinor);
					break;
					
				case ".svg":
					UpdateSvgContentVersion(path, versionMajorMinorAndBuild);
					break;
					
				case ".xml":
					if (Path.GetFileNameWithoutExtension(file) == "WMAppManifest")
						UpdateWMAppManifestContentVersion(path, versionMajorAndMinor);
					break;
				}
				
				WriteMessage(path);
			}
			
			WriteVersionFile(versionFile, fileList, major, minor, build, revision, startYear);
		}

		private void ReadVersionFile(
			string parsedPath, out string[] fileList, out int major, out int minor, out int build, out int revision, out int startYear)
		{
			XmlReader reader = null;
			
			using (reader = XmlReader.Create(parsedPath))
			{
				filesAtom = reader.NameTable.Add("Files");

				reader.MoveToContent();
				reader.ReadStartElement("Version");
				reader.MoveToContent();
				major = reader.ReadElementContentAsInt("Major", "");
				reader.MoveToContent();
				minor = reader.ReadElementContentAsInt("Minor", "");
				reader.MoveToContent();
				build = reader.ReadElementContentAsInt("Build", "");
				reader.MoveToContent();
				revision = reader.ReadElementContentAsInt("Revision", "");
				reader.MoveToContent();
				startYear = reader.ReadElementContentAsInt("StartYear", "");
				reader.MoveToContent();

				fileList = ReadFilesElement(reader);

				reader.ReadEndElement(); // Version
			}
		}

		private string[] ReadFilesElement(XmlReader reader)
		{
			List<string> fileList = new List<string>();

			reader.ReadStartElement(filesAtom);
			reader.MoveToContent();
			
			while (true)
			{
				if (String.ReferenceEquals(reader.Name, filesAtom))
				{
					reader.ReadEndElement();
					reader.MoveToContent();
					break;
				}

				string file = reader.ReadElementString("File");

#if MONOTOUCH
				file = file.Replace("\\", "/");
#else
				file = file.Replace("/", "\\");
#endif

				fileList.Add(file);
				reader.MoveToContent();
			}

			return fileList.ToArray();
		}

		private static void WriteVersionFile(string versionFile, string[] fileList, int major, int minor, int build, int revision, int startYear)
		{
			XmlWriterSettings settings = new XmlWriterSettings();
			
			settings.Indent = true;
			settings.IndentChars = "  ";
			
			using (XmlWriter writer = XmlWriter.Create(versionFile, settings))
			{
				writer.WriteStartElement("Version");
				writer.WriteElementString("Major", major.ToString());
				writer.WriteElementString("Minor", minor.ToString());
				writer.WriteElementString("Build", build.ToString());
				writer.WriteElementString("Revision", revision.ToString());
				writer.WriteElementString("StartYear", startYear.ToString());
				writer.WriteStartElement("Files");
				foreach (var file in fileList)
				{
					writer.WriteElementString("File", file);
				}
				writer.WriteEndElement(); // Files
				writer.WriteEndElement(); // Version
			}
		}
		
		static void UpdateSvgContentVersion(string file, string versionMajorMinorBuild)
		{
			string contents = File.ReadAllText(file);
			
			contents = Regex.Replace(
				contents,
				@"(?'before'VERSION )([0-9]+\.[0-9]+\.[0-9]+)",
				"${before}" + versionMajorMinorBuild);
			
			File.WriteAllText(file, contents);
		}
		
		static void UpdateWMAppManifestContentVersion(string file, string versionMajorMinor)
		{
			string contents = File.ReadAllText(file);
			
			contents = Regex.Replace(
				contents,
				@"(?'before'Version="")([0-9]+\.[0-9]+)(?'after'\.[0-9]+\.[0-9]+"")",
				"${before}" + versionMajorMinor + "${after}");
			
			File.WriteAllText(file, contents);
		}
		
		static void UpdateCSVersion(string file, string versionMajorMinor, string version)
		{
			string contents = File.ReadAllText(file);
			
			// Note that we use named substitutions because otherwise Regex gets confused.  "$1" + "1.0.0.0" = "$11.0.0.0".  There is no $11.
			
			contents = Regex.Replace(
				contents,
				@"(?'before'AssemblyVersion\("")([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)(?'after'""\))",
				"${before}" + versionMajorMinor + ".0.0${after}");
			
			contents = Regex.Replace(
				contents,
				@"(?'before'AssemblyFileVersion\("")([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)(?'after'""\))",
				"${before}" + version + "${after}");
			
			File.WriteAllText(file, contents);
		}
		
		static void UpdateRCVersion(string file, string version, string versionCsv)
		{
			string contents = File.ReadAllText(file);
			
			contents = Regex.Replace(
				contents,
				@"(?'before'FILEVERSION )([0-9]+,[0-9]+,[0-9]+,[0-9]+)",
				"${before}" + versionCsv);
			
			contents = Regex.Replace(
				contents,
				@"(?'before'PRODUCTVERSION )([0-9]+,[0-9]+,[0-9]+,[0-9]+)",
				"${before}" + versionCsv);
			
			contents = Regex.Replace(
				contents,
				@"(?'before'""FileVersion"",[ \t]*"")([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)(?'after'"")",
				"${before}" + version + "${after}");
			
			contents = Regex.Replace(
				contents,
				@"(?'before'""ProductVersion"",[ \t]*"")([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)(?'after'"")",
				"${before}" + version + "${after}");
			
			File.WriteAllText(file, contents);
		}
		
		static void UpdateWxiVersion(string file, string versionMajorMinor, string versionBuildAndRevision)
		{
			string contents = File.ReadAllText(file);
			
			contents = Regex.Replace(
				contents,
				@"(?'before'ProductVersion = "")([0-9]+\.[0-9]+)(?'after'"")",
				"${before}" + versionMajorMinor + "${after}");
			
			contents = Regex.Replace(
				contents,
				@"(?'before'ProductBuild = "")([0-9]+\.([0-9]|[1-9][0-9]))(?'after'"")",
				"${before}" + versionBuildAndRevision + "${after}");
			
			File.WriteAllText(file, contents);
		}
		
		static void UpdateConfigVersion(string file, string versionMajorMinor)
		{
			// In .config files we are looking for the section that contains an assembly reference 
			// for the section handler.
			string contents = File.ReadAllText(file);
			
			contents = Regex.Replace(
				contents,
				@"(?'before', +Version=)\d+\.\d+(?'after'\.0\.0 *,)",
				"${before}" + versionMajorMinor + "${after}");
			
			File.WriteAllText(file, contents);
		}
		
		static void UpdateProjVersion(string file, string version, string projectName)
		{
			string contents = File.ReadAllText(file);
			
			contents = Regex.Replace(
				contents,
				@"(?'before'<OutputName>" + projectName + @"_)([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)(?'after'</OutputName>)",
				"${before}" + version + "${after}");
			
			File.WriteAllText(file, contents);
		}
		
		static void UpdateVsixManifestVersion(string file, string version)
		{
			string contents = File.ReadAllText(file);
			
			contents = Regex.Replace(
				contents,
				@"(?'before'<Version>)([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)(?'after'</Version>)",
				"${before}" + version + "${after}");
			
			File.WriteAllText(file, contents);
		}
		
		private string GetProjectSolution()
		{
			string fileSpec = "*.sln";

			try
			{
				string dir = Environment.CurrentDirectory;

				do 
				{
					string[] files = Directory.GetFiles(dir, fileSpec);

					if (files.Length > 0)
					{
						return files[0];
					}

					int i = dir.LastIndexOf(Path.DirectorySeparatorChar);

					if (i == -1)
						break;

					dir = dir.Substring(0, i);
				}
				while (true);

				WriteError("Unable to find file '{0}' to determine project root", fileSpec);
			}
			catch (Exception e)
			{
				WriteError("Error looking for file '{0}'. {1}", fileSpec, e.Message);
			}

			return null;
		}
		
		static private int JDate(int startYear)
		{
			DateTime today = DateTime.Today;
			
			return (((today.Year - startYear + 1) * 10000) + (today.Month * 100) + today.Day);
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
					case 'q':
						NoLogo = true;
						continue;
					default:
						throw new ApplicationException(string.Format("Unknown argument '{0}'", arg[1]));
					}
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
	}
}

