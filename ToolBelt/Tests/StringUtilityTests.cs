using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using ToolBelt;
using NUnit.Framework;

namespace ToolBelt.Tests
{
	[TestFixture]
	public class StringUtilsTests 
	{
		[TestCase]
		public void TestWordWrap() 
		{
						  //1234567890123456789012345678901234567890123456789012345678901234567890
			string text1 = "This is some text that will be broken up onto word boundaries.";
			string[] lines = StringUtility.WordWrap(text1, 12);
			
			Assert.IsTrue(lines.Length == 7);	
			Assert.IsTrue(lines[2] == "that will ");
            Assert.IsTrue(Array.TrueForAll<string>(lines, delegate(string s) { return s.Length <= 12; }));

			              //1234567890123456789012345678901234567890123456789012345678901234567890
			string text2 = "Higgletypiggletywiggletywoo.  I wonder what this will do?";
			
			lines = StringUtility.WordWrap(text2, 12);
			
            Assert.IsTrue(lines.Length == 6);
            Assert.IsTrue(lines[3] == "wonder what ");
            Assert.IsTrue(Array.TrueForAll<string>(lines, delegate(string s) { return s.Length <= 12; }));

            //1234567890123456789012345678901234567890123456789012345678901234567890
            string text3 = "Something interesting: " + Environment.NewLine + "  This is the rest";

            lines = StringUtility.WordWrap(text3, 80);

            Assert.IsTrue(lines.Length == 2);
            Assert.IsTrue(Array.TrueForAll<string>(lines, delegate(string s) { return s.Length <= 80; }));
        }

        [TestCase]
        public void TestReplaceTags()
        {
            var tagsCaseSensitive = new Dictionary<string, string>(StringComparer.InvariantCulture);
            tagsCaseSensitive["foo1"] = "bar1";
            tagsCaseSensitive["foo2"] = "bar2";
            tagsCaseSensitive["FOO3"] = "bar3";

            var tagsCaseInsensitive = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            tagsCaseInsensitive["foo1"] = "bar1";
            tagsCaseInsensitive["foo2"] = "bar2";
            tagsCaseInsensitive["FOO3"] = "bar3";

            string prefix = "$(";
            string suffix = ")";
            string source = "$(foo1) $(foo2) $(foo3) $(foo1)$(foo2)$(foo3)$(foo1)";

            Assert.AreEqual("bar1 bar2 $(foo3) bar1bar2$(foo3)bar1", StringUtility.ReplaceTags(source, prefix, suffix, tagsCaseSensitive, TaggedStringOptions.LeaveUnknownTags));
            Assert.AreEqual("bar1 bar2 bar3 bar1bar2bar3bar1", StringUtility.ReplaceTags(source, prefix, suffix, tagsCaseInsensitive, TaggedStringOptions.LeaveUnknownTags));
            Assert.AreEqual("bar1 bar2  bar1bar2bar1", StringUtility.ReplaceTags(source, prefix, suffix, tagsCaseSensitive, TaggedStringOptions.RemoveUnknownTags));
            
            tagsCaseInsensitive.Clear();
            tagsCaseInsensitive["salt"] = "salt";
            prefix = "%";
            suffix = "%";
            source = "%salt%%salt%%pepper%%pepper%%salt%%pepper%";

            Assert.AreEqual("saltsaltsalt", StringUtility.ReplaceTags(source, prefix, suffix, tagsCaseInsensitive, TaggedStringOptions.RemoveUnknownTags));
        }
    }
}
