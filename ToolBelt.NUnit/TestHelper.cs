using System;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace ToolBelt.NUnit
{
    public static class TestHelper
    {
        public static void ProcessDeploymentItems(Type testClassType)
        {
            Dictionary<string, string> variables = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            // Add all environment variables
            foreach (DictionaryEntry pair in Environment.GetEnvironmentVariables())
            {
                variables[(string)pair.Key] = (string)pair.Value;
            }

            Assembly testAssembly = Assembly.GetAssembly(testClassType);
            string dirName = Path.GetDirectoryName(testAssembly.Location);
            string deploymentDir = dirName;
            variables["DeploymentDir"] = deploymentDir;

            while (true)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dirName);
                FileInfo[] fileInfo = dirInfo.GetFiles();
                var slnFileInfo = fileInfo.FirstOrDefault(f => String.Compare(Path.GetExtension(f.FullName), ".sln", true) == 0);

                if (slnFileInfo != null)
                    break;

                int n = dirName.LastIndexOf(Path.DirectorySeparatorChar);

                if (n == -1)
                {
                    dirName = "";
                    break;
                }

                dirName = dirName.Substring(0, n);
            }

            variables["SolutionDir"] = dirName;

            object[] attributes = testAssembly.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);

            if (attributes.Length > 0)
            {
                variables["Configuration"] = ((AssemblyConfigurationAttribute)attributes [0]).Configuration;
            }

            // Now find all the deployment attributes and do the deployments
            attributes = testClassType.GetCustomAttributes(typeof(DeploymentItemAttribute), false);

            StringBuilder sb = new StringBuilder();

            foreach (DeploymentItemAttribute attribute in attributes)
            {
                string filePath = attribute.FromFilePath;

                #if MACOS
                filePath = filePath.Replace(@"\", "/");
                #endif

                Regex re = new Regex(@"\$\(([^)]+)\)", RegexOptions.Multiline);
                int index = 0;
                Match m = re.Match(filePath, index);

                sb.Clear();

                while (m.Success)
                {
                    string value;

                    sb.Append(filePath.Substring(index, m.Index - index));

                    if (variables.TryGetValue(m.Groups[1].Value, out value))
                    {
                        sb.Append(value);
                    }

                    index = m.Index + m.Length;
                    m = m.NextMatch();
                }

                sb.Append(filePath.Substring(index));
                string fromFilePath = sb.ToString();
                
                if (!File.Exists(fromFilePath))
                    throw new FileNotFoundException(String.Format("Deployment file '{0}' does not exist", fromFilePath));

                string toFilePath = attribute.ToFilePath;

                if (String.IsNullOrEmpty(toFilePath))
                {
                    toFilePath = Path.GetFileName(fromFilePath);
                    #if MACOS
                    toFilePath = toFilePath.Replace(@"\", "/");
                    #endif
                }
                else if (Path.IsPathRooted(toFilePath))
                    throw new ArgumentException("To file name must not be rooted");

                toFilePath = Path.Combine(deploymentDir, toFilePath);

                string toFileDir = Path.GetDirectoryName(toFilePath);

                if (!Directory.Exists(toFileDir))
                   Directory.CreateDirectory(toFileDir);

                if (File.GetLastWriteTime(fromFilePath) > File.GetLastWriteTime(toFilePath))
                {
                    File.Copy(fromFilePath, toFilePath, true);
                    Console.WriteLine("{0} -> {1}", fromFilePath, toFilePath);
                }
            }
        }
    }
}
