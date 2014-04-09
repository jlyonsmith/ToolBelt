using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using NUnit.Framework;

namespace ToolBelt
{
    [TestFixture]
    public class CommandLineParserTests
    {
        class CustomType
        {
            public CustomType(Dictionary<string, string> parameters)
            {
                this.parameters = parameters;
            }

            public CustomType()
            {
                this.parameters = new Dictionary<string,string>();
            }

            public Dictionary<string, string> Parameters
            {
                get { return parameters; }
            }
            
            private Dictionary<string, string> parameters;
            
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                Dictionary<string, string>.Enumerator enumerator = parameters.GetEnumerator();
                bool more = enumerator.MoveNext();
                
                while (more)
                {
                    KeyValuePair<string, string> pair = enumerator.Current;
                    more = enumerator.MoveNext();
                    
                    sb.AppendFormat("{0}={1}{2}", pair.Key, pair.Value, more ? ";" : "");
                }
                
                return sb.ToString();
            }
        }

        static class CustomTypeInitializer
        {
            public static CustomType Parse(string data)
            {
                string[] entries = data.Split(';');

                Dictionary<string, string> dict = new Dictionary<string,string>(entries.Length);
                
                foreach (string entry in entries)
                {
                    string[] pair = entry.Split(new char[] {'='}, 2);
                    
                    if (pair.Length == 2)
                    {
                        dict.Add(pair[0], pair[1]);
                    }
                }
                
                return new CustomType(dict);
            }
        }

        [CommandLineTitle("Command Line Program")]
        [CommandLineDescription("A program that does something from the command line")]
        [CommandLineCopyright("Copyright (c) John Lyon-Smith")]
        [CommandLineConfiguration("Debug")]
        class ArgumentsBasic
        {
            string arg1;
            string[] arg2;
            List<string> arg3;
            bool arg4;
            int arg5;
            bool arg6;
            CustomType arg7;
            FileInfo arg8;
            ParsedPath arg9;
            private string arg10;
            private int arg11;
            string defArg;

            // Short name that is a prefix to the full name
            [CommandLineArgument("arg1", Description = "Argument #1", ShortName = "a")]
            public string Arg1
            {
                get { return arg1; }
                set { arg1 = value; }
            }

            // Short name that is not a prefix to the full name
            [CommandLineArgument("arg2", Description = "Argument #2", ShortName = "a2")]
            public string[] Arg2
            {
                get { return arg2; }
                set { arg2 = value; }
            }

            [CommandLineArgument("arg3", Description = "Argument #3", ValueHint = "<thing>")]
            public List<string> Arg3
            {
                get { return arg3; }
                set { arg3 = value; }
            }

            [CommandLineArgument("arg4", Description = "Argument #4")]
            public bool Arg4
            {
                get { return arg4; }
                set { arg4 = value; }
            }

            [CommandLineArgument("arg5", Description = "Argument #5")]
            public int Arg5
            {
                get { return arg5; }
                set { arg5 = value; }
            }

            [CommandLineArgument("arg6", Description = "Argument #6")]
            public bool Arg6
            {
                get { return arg6; }
                set { arg6 = value; }
            }

            [CommandLineArgument("arg7", Description = "Argument #7", Initializer = typeof(CustomTypeInitializer), MethodName="Parse")]
            public CustomType Arg7
            {
                get { return arg7; }
                set { arg7 = value; }
            }

            [CommandLineArgument("arg8", Description = "Argument #8")]
            public FileInfo Arg8
            {
                get { return arg8; }
                set { arg8 = value; }
            }

            [CommandLineArgument("arg9", Description = "Argument #9")]
            public ParsedPath Arg9
            {
                get { return arg9; }
                set { arg9 = value; }
            }
            
            // This is here so we can see what happens with unset arguments
            [CommandLineArgument("arg10", Description = "Argument #10")]
            public string Arg10
            {
                get { return arg10; }
                set { arg10 = value; }
            }

            // Another argument that's not meant to be set
            [CommandLineArgument("arg11", Description = "Argument #11")]
            public int Arg11
            {
                get { return arg11; }
                set { arg11 = value; }
            }

            [DefaultCommandLineArgument("default")]
            public string Default
            {
                get { return defArg; }
                set { defArg = value; }
            }
        }

        class ArgumentsFromResources
        {
            private string file;

            [CommandLineArgument("arg1", Description = "SwitchArg1Description", ShortName = "a", ValueHint = "SwitchArg1ValueHint")]
            public string File
            {
                get { return file; }
                set { file = value; }
            }
        }

        class ArgumentsMultiDefaultNoArgs
        {
            [DefaultCommandLineArgument("default", ValueHint = "<file>")]
            public string[] Default
            {
                get { return defArgs; }
                set { defArgs = value; }
            }

            private string[] defArgs;
        }

        class ArgumentsBasicTarget : ArgumentsBasic
        {
        }

        class ArgumentsForLogoBanner
        {
            string arg1;

            [CommandLineArgument("arg1", Description = "Argument #1")]
            public string Arg1
            {
                get { return arg1; }
                set { arg1 = value; }
            }
        }

        class ArgumentsCaseSensitive
        {
            string arg1;
            string arg2;

            [CommandLineArgument("Tp", Description = "Argument #1")]
            public string Arg1
            {
                get { return arg1; }
                set { arg1 = value; }
            }

            // Short name that is a prefix to the full name
            [CommandLineArgument("TP", Description = "Argument #2")]
            public string Arg2
            {
                get { return arg2; }
                set { arg2 = value; }
            }
        }

        class ArgumentsTooManyDefaults
        {
            string defArg1;
            string defArg2;

            [DefaultCommandLineArgument("default")]
            public string Default1
            {
                get { return defArg1; }
                set { defArg1 = value; }
            }

            [DefaultCommandLineArgument("default")]
            public string Default2
            {
                get { return defArg2; }
                set { defArg2 = value; }
            }
        }

        class ArgumentsBadDerivation
        {
        }

        class ArgumentsPropertyNotReadWrite
        {
            [CommandLineArgument("bad", Description = "Bad argument property")]
            public bool Bad
            {
                get { return false; }
            }
        }

        class ArgumentsBadUnprocessedPart
        {
            private string[] unprocessed;

            [UnprocessedCommandLineArgument("unprocessed")]
            public string[] Unprocessed
            {
                get { return unprocessed; }
                set { unprocessed = value; }
            }
        }

        class ArgumentsUnprocessedPart
        {
            private string arg;
            private string[] unprocessed;
            private string def;

            [CommandLineArgument("arg", Description = "Only argument")]
            public string Arg
            {
                get { return arg; }
                set { arg = value; }
            }

            [DefaultCommandLineArgument("thing", Description = "<thing>")]
            public string Default
            {
                get { return def; }
                set { def = value; }
            }

            [UnprocessedCommandLineArgument("unprocessed")]
            public string[] Unprocessed
            {
                get { return unprocessed; }
                set { unprocessed = value; }
            }
        }

        class ArgumentsNoDescription
        {
            string arg1;

            [CommandLineArgument("arg1")]
            public string Arg1
            {
                get { return arg1; }
                set { arg1 = value; }
            }
        }

        [CommandLineDescription("Command Line Program")]
        [CommandLineCopyright("Copyright (c) John Lyon-Smith")]
        [CommandLineConfiguration("Debug")]
        [CommandLineCommandDescription("start", Description = "Starts the program, but we need some more text so that we can ensure that the description is wrapped so here it is.")]
        [CommandLineCommandDescription("stop", Description = "Stops the program.")]
        [CommandLineCommandDescription("pause", Description = "Pauses the program, and again we need to ensure that word wrapping is occurring properly so I'll add sum cruft.")]
        [CommandLineCommandDescription("help", Description = "Provides help on a specific command")]
        class ArgumentsWithCommand
        {
            string arg1;
            string[] arg2;
            bool arg3;
            bool help;
            string command;
            string defArg;

            // Short name that is a prefix to the full name
            [CommandLineArgument("arg1", Description = "Argument #1", ShortName = "a", Commands = "stop,pause")]
            public string Arg1
            {
                get { return arg1; }
                set { arg1 = value; }
            }

            // Short name that is not a prefix to the full name
            [CommandLineArgument("arg2", Description = "Argument #2", ShortName = "a2", Commands = "start,stop")]
            public string[] Arg2
            {
                get { return arg2; }
                set { arg2 = value; }
            }

            [CommandLineArgument("arg3", Description = "Argument #3", Commands = "pause")]
            public bool Arg3
            {
                get { return arg3; }
                set { arg3 = value; }
            }

            [CommandLineArgument("help", Description = "Help", Commands = ",start,stop,pause")]
            public bool Help
            {
                get { return help; }
                set { help = value; }
            }

            [CommandCommandLineArgument("Command", Commands = ",start,stop,pause,help")]
            public string Command
            {
                get { return command; }
                set { command = value; }
            }
            
            [DefaultCommandLineArgument("Default", Commands = "start,stop")]
            public string Default
            {
                get { return defArg; }
                set { defArg = value; }
            }
        }
    
        [Test]
        public void TestBasicParsing()
        {
            ArgumentsBasicTarget target = new ArgumentsBasicTarget();
            CommandLineParser parser = new CommandLineParser(typeof(ArgumentsBasic), null, CommandLineParserFlags.Default);
            string[] args = new string[] 
            { 
                "-arg1:valueForArg1", 
                "-a2:One", 
                "-arg2:Two", 
                "-arg3:Alpha", 
                "-arg3:Beta", 
                "-arg4+", 
                "-arg5:10", 
                "-arg7:a=1;b=2",
                "-arg8:blah.txt",
                "-arg9:blah.txt",
                "something.txt"             
            };
            
            parser.ParseAndSetTarget(args, target);

            Assert.AreEqual(target.Arg1, "valueForArg1");
            CollectionAssert.AreEqual(new string[] { "One", "Two" }, target.Arg2);
            CollectionAssert.AreEqual(new string[] { "Alpha", "Beta" }, target.Arg3);
            Assert.AreEqual(true, target.Arg4);
            Assert.AreEqual(10, target.Arg5);
            Assert.AreEqual(false, target.Arg6);
            CollectionAssert.AreEquivalent(new KeyValuePair<string, string>[] 
                { new KeyValuePair<string, string>("a", "1"), new KeyValuePair<string, string>("b", "2") }, target.Arg7.Parameters);
            Assert.AreEqual("blah.txt", target.Arg8.ToString());
            Assert.AreEqual("blah.txt", target.Arg9.ToString());
            Assert.AreEqual(11, parser.ArgumentCount);
            Assert.AreEqual(" -arg1:valueForArg1 -arg2:One -arg2:Two -arg3:Alpha -arg3:Beta -arg4 -arg5:10 -arg7:a=1;b=2 -arg8:blah.txt -arg9:blah.txt something.txt", parser.Arguments);
        }

        [Test]
        public void TestGetTarget()
        {
            ArgumentsBasicTarget target = new ArgumentsBasicTarget();
            CommandLineParser parser = new CommandLineParser(typeof(ArgumentsBasic), null, CommandLineParserFlags.Default);

            target.Arg1 = "valueForArg1";
            target.Arg2 = new string[] { "One", "Two" };
            
            List<string> arg3 = new List<string>();
            arg3.Add("Alpha");
            arg3.Add("Beta");
            
            target.Arg3 = arg3;
            target.Arg4 = true;
            target.Arg5 = 10;
            target.Arg6 = false;
            
            CustomType arg7 = new CustomType();
            
            arg7.Parameters.Add("a", "1");
            arg7.Parameters.Add("b", "2");
            
            target.Arg7 = arg7;
            target.Arg8 = new FileInfo("blah.txt");
            target.Arg9 = new ParsedPath("blah.txt", PathType.File);
            target.Default = "something.txt"; 

            parser.GetTargetArguments(target);
            
            // NOTE: Not setting arguments #10 and #11

            Assert.AreEqual(13, parser.ArgumentCount);
            Assert.AreEqual(" -arg1:valueForArg1 -arg2:One -arg2:Two -arg3:Alpha -arg3:Beta -arg4 -arg5:10 -arg7:a=1;b=2 -arg8:blah.txt -arg9:blah.txt -arg11:0 something.txt", parser.Arguments);
        }
        
        [Test]
        public void TestCaseSensitiveParsing()
        {
            ArgumentsCaseSensitive target = new ArgumentsCaseSensitive();
            CommandLineParser parser = new CommandLineParser(typeof(ArgumentsCaseSensitive), null, CommandLineParserFlags.CaseSensitive);
            string[] args = new string[] 
            { 
                "-Tp:valueForArg1", 
                "-TP:valueForArg2", 
            };

            parser.ParseAndSetTarget(args, target);

            Assert.AreEqual(target.Arg1, "valueForArg1");
            Assert.AreEqual(target.Arg2, "valueForArg2");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestBadUnprocessedArgs()
        {
            ArgumentsBadUnprocessedPart target = new ArgumentsBadUnprocessedPart();
            new CommandLineParser(target.GetType());
        }
        
        [Test]
        public void TestUnprocessedArgs()
        {
            ArgumentsUnprocessedPart target = new ArgumentsUnprocessedPart();
            CommandLineParser parser = new CommandLineParser(
                typeof(ArgumentsUnprocessedPart));
                
            string[] args = new string[]
            {
                "-arg:blah", "thing", "gomez", "-morestuff"
            };

            parser.ParseAndSetTarget(args, target);
            
            Assert.AreEqual("blah", target.Arg);
            Assert.AreEqual("thing", target.Default);
            CollectionAssert.AreEqual(new string[] { "gomez", "-morestuff" }, target.Unprocessed);
        }
        
        [Test]
        public void TestMultiDefaultArgs()
        {
            ArgumentsMultiDefaultNoArgs target = new ArgumentsMultiDefaultNoArgs();
            CommandLineParser parser = new CommandLineParser(typeof(ArgumentsMultiDefaultNoArgs), null, CommandLineParserFlags.Default);
            string[] args = new string[] { "one", "two"};

            parser.ParseAndSetTarget(args, target);

            CollectionAssert.AreEqual(new string[] { "one", "two" }, target.Default);
        }
        
        [Test]
        public void TestLogoBanner()
        {
            Regex regex = new Regex(
                @"^Command Line Program\. (?:Debug|Release) Version \d+\.\d+\.\d+\.\d+" + Environment.NewLine + 
                @"Copyright \(c\) John Lyon-Smith\." + Environment.NewLine, 
                RegexOptions.Multiline);
            
            CommandLineParser parser = new CommandLineParser(typeof(ArgumentsBasic), null, CommandLineParserFlags.Default);
            
            string logoBanner = parser.LogoBanner;

            Assert.IsTrue(regex.IsMatch(logoBanner));

            parser.Copyright = "Copyright (c) Another Corporation";
            parser.Title = "Another Command Line Program";
            parser.Configuration = "Release";
            parser.Version = "2.0.0.0";

            logoBanner = parser.LogoBanner;

            regex = new Regex(
                @"^.+?\. Release Version \d+\.\d+\.\d+\.\d+" + Environment.NewLine + 
                @"Copyright \(c\) .+?" + Environment.NewLine,
                RegexOptions.Multiline | RegexOptions.ExplicitCapture);

            Assert.IsTrue(regex.IsMatch(logoBanner));
            
            // A parser with no attributes to get the code coverage
            parser = new CommandLineParser(typeof(ArgumentsForLogoBanner));
            
            logoBanner = parser.LogoBanner;

            regex = new Regex(
                @"^.+?\. (Debug|Release) Version \d+\.\d+\.\d+\.\d+" + Environment.NewLine + 
                @"Copyright \(c\) .+?" + Environment.NewLine,
                RegexOptions.Multiline | RegexOptions.ExplicitCapture);
    
            Assert.IsTrue(regex.IsMatch(logoBanner));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestNoSpecificationType()
        {
            new CommandLineParser(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestArgumentNoDescription()
        {
            ArgumentsBasicTarget target = new ArgumentsBasicTarget();
            // The target object is not derived from the given argument specification type...
            new CommandLineParser(typeof(ArgumentsNoDescription)).ParseAndSetTarget(null, target);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestParserNoTarget()
        {
            new CommandLineParser(typeof(ArgumentsBasic)).ParseAndSetTarget(null, null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestBadTargetDerivation()
        {
            ArgumentsBadDerivation target = new ArgumentsBadDerivation();
            new CommandLineParser(typeof(ArgumentsBasic)).ParseAndSetTarget(null, target);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestTooManyDefaultAttributes()
        {
            new CommandLineParser(typeof(ArgumentsTooManyDefaults)).Parse(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestBadProperty()
        {
            ArgumentsPropertyNotReadWrite target = new ArgumentsPropertyNotReadWrite();
            new CommandLineParser(target.GetType());
        }

        [Test]
        public void TestUsage()
        {
            CommandLineParser parser = new CommandLineParser(typeof(ArgumentsBasic), CommandLineParserFlags.Default);

            string usage = parser.Usage;
            string nl = Environment.NewLine;

            Assert.IsTrue(Regex.IsMatch(
                usage,
                @"Syntax:\s*.+? \[switches\] <default>" + nl + nl + 
                @"Description:\s*.*" + nl + nl +
                @"Switches:" + nl + nl + 
                @"(^(/\w+)|(\s+).*" + nl + nl + 
                @")+(.*" + nl + 
                @")+",
                RegexOptions.Multiline | RegexOptions.ExplicitCapture));
            
            parser.CommandName = "AnythingYouWant";
            
            Assert.AreEqual(parser.CommandName, "AnythingYouWant");
            
            // Test multiple default arguments
            parser = new CommandLineParser(typeof(ArgumentsMultiDefaultNoArgs), null, CommandLineParserFlags.Default);

            Assert.IsTrue(Regex.IsMatch(parser.Usage,
                @"Syntax:\s*.+? <file> \[<file> \.\.\.\]",
                RegexOptions.Multiline | RegexOptions.ExplicitCapture));
                
            // Test command based command line
            parser = new CommandLineParser(typeof(ArgumentsWithCommand));

            // Capture the value or it's really hard to debug...
            usage = parser.GetUsage(null, 79);
    
            Debug.WriteLine(usage);

            Assert.IsTrue(Regex.IsMatch(
                usage,
                @"Syntax:\s*.+? <command> \.\.\." + nl + nl +
                @"Description:\s*.+" + nl + nl +
                @"Commands:" + nl + nl +
                @"((^  \w+\s+\S+" + nl + 
                @")|(^\s+.+" + nl + 
                @"))+(^\s+.+)",
                RegexOptions.Multiline | RegexOptions.ExplicitCapture));

            // Get usage for individual command
            usage = parser.GetUsage("start", 79);

            Debug.WriteLine(usage);

            Assert.IsTrue(Regex.IsMatch(
                usage,
                @"Syntax:\s*.+? start \[switches\] <default>" + nl + nl +
                @"Description:\s*.+" + nl + 
                @"(^\s+.+" + nl + 
                @")+" + nl + 
                @"Switches:" + nl + 
                @"((^  \w+\s+\S+" + nl + 
                @")|(^\s+.+" + nl + 
                @"))+(^\s+.+)",
                RegexOptions.Multiline | RegexOptions.ExplicitCapture));

            ArgumentsWithCommand target = new ArgumentsWithCommand();
            string[] args = { "help", "unknown" };

            parser.ParseAndSetTarget(args, target);

            Assert.AreEqual("help", target.Command);
            Assert.AreEqual("unknown", target.Default);

            try
            {
                parser.GetUsage(target.Default);
            }
            catch (Exception e)
            {
                Assert.IsInstanceOf(typeof(CommandLineArgumentException), e);
            }
        }

        [Test]
        [ExpectedException(typeof(CommandLineArgumentException))]
        public void TestBadCommand()
        {
            ArgumentsWithCommand target = new ArgumentsWithCommand();
            CommandLineParser parser = new CommandLineParser(target.GetType());
            string[] args = { "unknown" };

            parser.Parse(args);
        }

        [Test]
        [ExpectedException(typeof(CommandLineArgumentException))]
        public void TestBadCommandArgument()
        {
            string[] args = { "start", "-arg1" };

            CommandLineParser parser = new CommandLineParser(typeof(ArgumentsWithCommand));

            parser.Parse(args);
        }

        [Test]
        public void TestParsingFromResources()
        {
            ArgumentsFromResources target = new ArgumentsFromResources();
            CommandLineParser parser = new CommandLineParser(
                typeof(ArgumentsFromResources), typeof(CommandLineParserTestsResources));
            string[] args = new string[] { "-a:file.txt" };

            parser.ParseAndSetTarget(args, target);

            Assert.AreEqual("file.txt", target.File);
            Assert.IsTrue(Regex.IsMatch(
                parser.Usage,
                @"Syntax:\s+.+? \[switches\]" + Environment.NewLine + Environment.NewLine + 
                @"Switches:" + Environment.NewLine + Environment.NewLine + @"(^  -\w+.*)",
                RegexOptions.Multiline | RegexOptions.ExplicitCapture));
        }
        
        [Test]
        public void TestCommandLineArgumentException()
        {
            Assert.DoesNotThrow(delegate { new CommandLineArgumentException(); });
            Assert.DoesNotThrow(delegate { new CommandLineArgumentException("A message"); });
            Assert.DoesNotThrow(delegate { new CommandLineArgumentException("Another message", new SystemException()); });
        }

        [Test]
        public void TestCommands()
        {
            ArgumentsWithCommand target = new ArgumentsWithCommand();
            CommandLineParser parser = new CommandLineParser(typeof(ArgumentsWithCommand));
            string[] args1 = new string[]
            {
                "start",
                "default",
                "-arg2:blah"
            };
            
            parser.ParseAndSetTarget(args1, target);
            
            Assert.AreEqual("start", target.Command);
            Assert.AreEqual("default", target.Default);
            CollectionAssert.AreEquivalent(new string[] {"blah"}, target.Arg2);
        }
    }
}
