using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using NUnit.Framework;

namespace ToolBelt.Tests
{
    [TestFixture]
    public class CommandLineParserTests
    {
        [CommandLineTitle("Command Line Program")]
        [CommandLineDescription("A program that does something from the command line")]
        [CommandLineCopyright("Copyright (c) John Lyon-Smith")]
        [CommandLineConfiguration("Debug")]
        class ArgumentsBasic
        {
            string arg1;
            List<int> arg2;
            List<string> arg3;
            bool arg4;
            int arg5;
            bool arg6;
            CustomType arg7;
            FileInfo arg8;
            ParsedPath arg9;
            private string arg10;
            private int arg11;
            int? arg12;
            string defArg;

            // Short name that is a prefix to the full name
            [CommandLineArgument("arg1", Description = "Argument #1", ShortName = "a")]
            public string Arg1
            {
                get { return arg1; }
                set { arg1 = value; }
            }

            [CommandLineArgument("arg2", Description = "Argument #2", ShortName="a2", ValueHint = "<num>")]
            public List<int> Arg2
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

            // Test Nullable types
            [CommandLineArgument("arg12", Description = "Argument #12")]
            public int? Arg12 
            { 
                get { return arg12; }
                set { arg12 = value; }
            }

            [DefaultCommandLineArgument()]
            public string Default
            {
                get { return defArg; }
                set { defArg = value; }
            }
        }

        class ArgumentsWithArray
        {
            // Short name that is not a prefix to the full name
            [CommandLineArgument("arg2", Description = "Argument #2", ShortName = "a2")]
            public string[] Arg2 { get; set; }
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
            [DefaultCommandLineArgument(ValueHint = "<file>")]
            public List<string> Default
            {
                get { return defArgs; }
                set { defArgs = value; }
            }

            private List<string> defArgs;
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

            [DefaultCommandLineArgument()]
            public string Default1
            {
                get { return defArg1; }
                set { defArg1 = value; }
            }

            [DefaultCommandLineArgument()]
            public string Default2
            {
                get { return defArg2; }
                set { defArg2 = value; }
            }
        }

        class ArgumentsNoAttributes
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
            private List<string> unprocessed;

            [UnprocessedCommandLineArgument()]
            public List<string> Unprocessed
            {
                get { return unprocessed; }
                set { unprocessed = value; }
            }
        }

        class ArgumentsUnprocessedPart
        {
            private string arg;
            private List<string> unprocessed;
            private string def;

            [CommandLineArgument("arg", Description = "Only argument")]
            public string Arg
            {
                get { return arg; }
                set { arg = value; }
            }

            [DefaultCommandLineArgument(Description = "<thing>")]
            public string Default
            {
                get { return def; }
                set { def = value; }
            }

            [UnprocessedCommandLineArgument()]
            public List<string> Unprocessed
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
            List<string> arg2;
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
            public List<string> Arg2
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

            [CommandCommandLineArgument(Commands = ",start,stop,pause,help")]
            public string Command
            {
                get { return command; }
                set { command = value; }
            }
            
            [DefaultCommandLineArgument(Commands = "start,stop")]
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
            CommandLineParser parser = new CommandLineParser(target, flags: CommandLineParserFlags.Default);
            string[] args = new string[] 
            { 
                "-arg1:valueForArg1", 
                "-a2:1", 
                "-arg2:2", 
                "-arg3:Alpha", 
                "-arg3:Beta", 
                "-arg4+", 
                "-arg5:10", 
                "-arg7:a=1;b=2",
                "-arg8:blah.txt",
                "-arg9:blah.txt",
                "something.txt"             
            };
            
            parser.ParseAndSetTarget(args);

            Assert.AreEqual(target.Arg1, "valueForArg1");
            CollectionAssert.AreEqual(new List<int> { 1, 2 }, target.Arg2);
            CollectionAssert.AreEqual(new string[] { "Alpha", "Beta" }, target.Arg3);
            Assert.AreEqual(true, target.Arg4);
            Assert.AreEqual(10, target.Arg5);
            Assert.AreEqual(false, target.Arg6);
            CollectionAssert.AreEquivalent(new KeyValuePair<string, string>[] 
                { new KeyValuePair<string, string>("a", "1"), new KeyValuePair<string, string>("b", "2") }, target.Arg7.Parameters);
            Assert.AreEqual("blah.txt", target.Arg8.ToString());
            Assert.AreEqual("blah.txt", target.Arg9.ToString());
            Assert.AreEqual(
                " -arg1:valueForArg1 -arg2:1 -arg2:2 -arg3:Alpha -arg3:Beta -arg4 -arg5:10 -arg7:a=1;b=2 -arg8:blah.txt -arg9:blah.txt -arg11:0 something.txt", 
                parser.GetArgumentString());
            Assert.IsNull(target.Arg12);
        }

        [Test]
        public void TestNullable()
        {
            ArgumentsBasicTarget target = new ArgumentsBasicTarget();
            CommandLineParser parser = new CommandLineParser(target, flags: CommandLineParserFlags.Default);
            string[] args = new string[] 
            { 
                "-arg12:10"
            };

            parser.ParseAndSetTarget(args);
            Assert.AreEqual(10, target.Arg12);
        }

        [Test]
        public void TestCaseSensitiveParsing()
        {
            ArgumentsCaseSensitive target = new ArgumentsCaseSensitive();
            CommandLineParser parser = new CommandLineParser(target, null, CommandLineParserFlags.CaseSensitive);
            string[] args = new string[] 
            { 
                "-Tp:valueForArg1", 
                "-TP:valueForArg2", 
            };

            parser.ParseAndSetTarget(args);

            Assert.AreEqual(target.Arg1, "valueForArg1");
            Assert.AreEqual(target.Arg2, "valueForArg2");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestBadUnprocessedArgs()
        {
            ArgumentsBadUnprocessedPart target = new ArgumentsBadUnprocessedPart();
            new CommandLineParser(target);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestArgIsArray()
        {
            var target = new ArgumentsWithArray();
            new CommandLineParser(target);
        }

        [Test]
        public void TestUnprocessedArgs()
        {
            ArgumentsUnprocessedPart target = new ArgumentsUnprocessedPart();
            CommandLineParser parser = new CommandLineParser(target);
                
            string[] args = new string[]
            {
                "-arg:blah", "thing", "gomez", "-morestuff"
            };

            parser.ParseAndSetTarget(args);
            
            Assert.AreEqual("blah", target.Arg);
            Assert.AreEqual("thing", target.Default);
            CollectionAssert.AreEqual(new string[] { "gomez", "-morestuff" }, target.Unprocessed);
        }
        
        [Test]
        public void TestMultiDefaultArgs()
        {
            ArgumentsMultiDefaultNoArgs target = new ArgumentsMultiDefaultNoArgs();
            CommandLineParser parser = new CommandLineParser(target, flags: CommandLineParserFlags.Default);
            string[] args = new string[] { "one", "two"};

            parser.ParseAndSetTarget(args);

            CollectionAssert.AreEqual(new string[] { "one", "two" }, target.Default);
        }
        
        [Test]
        public void TestLogoBanner()
        {
            // NOTE: This test requires the test assembly has version information
            //
            Regex regex = new Regex(
                @"^Command Line Program\. (?:Debug|Release) Version \d+\.\d+\.\d+\.\d+" + Environment.NewLine + 
                @"Copyright \(c\) John Lyon-Smith\." + Environment.NewLine, 
                RegexOptions.Multiline);
            
            CommandLineParser parser = new CommandLineParser(new ArgumentsBasic(), flags: CommandLineParserFlags.Default);
            
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
            parser = new CommandLineParser(new ArgumentsForLogoBanner());
            
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
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestParserNoTarget()
        {
            new CommandLineParser(null).ParseAndSetTarget(null);
        }

        [Test]
        public void TestNoAttributes()
        {
            new CommandLineParser(new ArgumentsNoAttributes()).ParseAndSetTarget(new string[] {});
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestTooManyDefaultAttributes()
        {
            new CommandLineParser(new ArgumentsTooManyDefaults()).ParseAndSetTarget(new string[0]);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestBadProperty()
        {
            ArgumentsPropertyNotReadWrite target = new ArgumentsPropertyNotReadWrite();
            new CommandLineParser(target);
        }

        [Test]
        public void TestUsage()
        {
            CommandLineParser parser = new CommandLineParser(new ArgumentsBasic(), flags: CommandLineParserFlags.Default);

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
            parser = new CommandLineParser(new ArgumentsMultiDefaultNoArgs(), null, CommandLineParserFlags.Default);

            Assert.IsTrue(Regex.IsMatch(parser.Usage,
                @"Syntax:\s*.+? <file> \[<file> \.\.\.\]",
                RegexOptions.Multiline | RegexOptions.ExplicitCapture));
                
            // Test command based command line
            parser = new CommandLineParser(new ArgumentsWithCommand());

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

            var target = new ArgumentsWithCommand();

            parser = new CommandLineParser(target);

            string[] args = { "help", "unknown" };

            parser.ParseAndSetTarget(args);

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
            CommandLineParser parser = new CommandLineParser(new ArgumentsWithCommand());
            string[] args = { "unknown" };

            parser.ParseAndSetTarget(args);
        }

        [Test]
        [ExpectedException(typeof(CommandLineArgumentException))]
        public void TestBadCommandArgument()
        {
            string[] args = { "start", "-arg1" };

            CommandLineParser parser = new CommandLineParser(new ArgumentsWithCommand());

            parser.ParseAndSetTarget(args);
        }

        [Test]
        public void TestParsingFromResources()
        {
            ArgumentsFromResources target = new ArgumentsFromResources();
            CommandLineParser parser = new CommandLineParser(target, typeof(CommandLineParserTestsResources));
            string[] args = new string[] { "-a:file.txt" };

            parser.ParseAndSetTarget(args);

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
        public void TestNoDuplicates()
        {
            var parser = new CommandLineParser(new ArgumentsBasic());

            Assert.Throws<CommandLineArgumentException>(() => parser.ParseAndSetTarget(new string[] { "-arg1:abc", "-arg1:def" }));
        }

        [Test]
        public void TestCommands()
        {
            ArgumentsWithCommand target = new ArgumentsWithCommand();
            CommandLineParser parser = new CommandLineParser(target);
            string[] args1 = new string[]
            {
                "start",
                "default",
                "-arg2:blah"
            };
            
            parser.ParseAndSetTarget(args1);
            
            Assert.AreEqual("start", target.Command);
            Assert.AreEqual("default", target.Default);
            CollectionAssert.AreEquivalent(new string[] {"blah"}, target.Arg2);
        }
    }
}
