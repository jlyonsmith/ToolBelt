using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Globalization;

namespace ToolBelt
{
    /// <summary>
    /// Flags for TaggedString operations
    /// </summary>
    [Flags]
    public enum TaggedStringOptions
    {
        /// <summary>
        /// Search case insensitively and leave unknown tags in the output
        /// </summary>
        LeaveUnknownTags = 0x00,

        /// <summary>
        /// Tags that are not found in the dictionary are replaced with an empty string
        /// instead of being left in the output.
        /// </summary>
        RemoveUnknownTags = 0x01,

        /// <summary>
        /// Throw exception on unknown tags
        /// </summary>
        ThrowOnUnknownTags = 0x02,
    }

    /// <summary>
    /// Groups a set of useful <see cref="string" /> manipulation and validation
    /// methods.
    /// </summary>
    public static class StringUtility
    {
        /// <summary>
        /// Converts an empty string ("") to <see langword="null" />.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        /// <see langword="null" /> if <paramref name="value" /> is an empty 
        /// string ("") or <see langword="null" />; otherwise, <paramref name="value" />.
        /// </returns>
        public static string ConvertEmptyToNull(string value) 
        {
            if (!String.IsNullOrEmpty(value)) 
            {
                return value;
            }

            return null;
        }

        /// <summary>
        /// Converts <see langword="null" /> to an empty string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        /// An empty string if <paramref name="value" /> is <see langword="null" />;
        /// otherwise, <paramref name="value" />.
        /// </returns>
        public static string ConvertNullToEmpty(string value) 
        {
            if (value == null) 
            {
                return string.Empty;
            }

            return value;
        }

        /// <summary>
        /// Concatenates a specified separator <see cref="string" /> between each 
        /// element of a specified list, yielding a single concatenated string.
        /// </summary>
        /// <param name="separator">A <see cref="string" />.</param>
        /// <param name="value">A string collection.</param>
        /// <returns>
        /// A <see cref="string" /> consisting of the elements of <paramref name="value" /> 
        /// interspersed with the separator string.
        /// </returns>
        /// <remarks>
        /// <para>
        /// For example if <paramref name="separator" /> is ", " and the elements 
        /// of <paramref name="value" /> are "apple", "orange", "grape", and "pear", 
        /// the method returns "apple, orange, grape, pear".
        /// </para>
        /// <para>
        /// If <paramref name="separator" /> is <see langword="null" />, an empty 
        /// string (<see cref="string.Empty" />) is used instead.
        /// </para>
        /// </remarks>
        public static string Join(string separator, IList<string> value)
        {
            if (value == null) 
            {
                throw new ArgumentNullException("value");
            }

            if (separator == null) 
            {
                separator = string.Empty;
            }

            // create with size equal to number of elements in collection
            string[] elements = new string[value.Count];

            // copy elements in collection to array       
            value.CopyTo(elements, 0);

            // concatenate specified separator between each elements 
            return string.Join(separator, elements);
        }

        /// <summary>
        /// Breaks a string on word boundaries, to the given <paramref name="lineLength"/>.  
        /// Only space characters are considered when breaking up lines.  
        /// Embedded <see cref="Environment.NewLine"/> sequences will break a line and are not included in the 
        /// resulting output.
        /// Each line is broken after the first whitespace after the last word on the line, so all space 
        /// characters are preserved in the resulting output.
        /// </summary>
        /// <param name="text">The text to break up.</param>
        /// <param name="lineLength">The maximum length of a line</param>
        /// <returns>An array of strings no longer than the given line length</returns>
        public static string[] WordWrap(this string text, int lineLength)
        {
			Debug.Assert(lineLength > 0);

			List<string> lines = new List<string>();
            
            // First, split the string up on any newline boundaries
            lines.AddRange(text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None));
            
            // Then further sub-divide the lines on word break boundaries
            int i = 0;
            
            while (i < lines.Count)
            {
                while (lines[i].Length > lineLength)
                {
                    string line = lines[i];
                    int e = lineLength - 1;

                    // If we hit a space, break right here
                    if (line[e] != ' ')
                    {
                        while (!Char.IsWhiteSpace(line[e]) && e > 0)
                            e--;

                        // We couldn't find any whitespace, so just use truncate to the end of the line
                        if (e == 0)
                            e = lineLength - 1;
                    }

                    lines[i] = line.Substring(e + 1);
                    lines.Insert(i, line.Substring(0, e + 1));
                    i++;
                }
                
                i++;
            }

            return lines.ToArray();
        }

        /// <summary>Format a string using the invariant culture. Use for persisted strings not returned to user.
        /// </summary>
        /// <see>string.Format</see> for argument format.
        public static string InvariantFormat(this string format, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }

        /// <summary> Format a string using the current culture. Use for strings returned to user.</summary>
        /// <see>string.Format</see> for argument format.
        public static string CultureFormat(this string format, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, format, args);
        }

        /// <summary>
        /// This method searches for each occurrence of a tagged variable in <c>source</c> and 
        /// replaces it with the value from the a dictionary <c>subs</c>.  Comparisons are done case
        /// insensitively. 
        /// </summary>
        /// <param name="source">String containing tagged entities</param>
        /// <param name="tagPrefix">The tag prefix</param>
        /// <param name="tagSuffix">The tag suffix</param>
        /// <param name="dictionary">A dictionary of tag values</param>
        /// <returns>A string with all tags replaced</returns>
        public static string ReplaceTags(this string source, string tagPrefix, string tagSuffix, IDictionary dictionary)
        {
            return ReplaceTags(source, tagPrefix, tagSuffix, dictionary, TaggedStringOptions.LeaveUnknownTags);
        }

        /// <summary>
        /// This method searches for each occurrence of a tagged variable in <c>source</c> and 
        /// replaces it with the value from the a dictionary <c>subs</c>.  Comparisons are done case
        /// insensitively. 
        /// </summary>
        /// <param name="source">String containing tagged entities</param>
        /// <param name="dict">A dictionary of tag values</param>
        /// <param name="tagPrefix">The tag prefix</param>
        /// <param name="tagSuffix">The tag suffix</param>
        /// <param name="flags"><see cref="TaggedStringOptions"/> for the replace operation</param>
        /// <returns>A string with all tags replaced</returns>
        public static string ReplaceTags(
            this string source, string tagPrefix, string tagSuffix, IDictionary dictionary, TaggedStringOptions flags)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            StringBuilder sb = new StringBuilder(source);

            for (int index = source.Length - 1; index != -1; )
            {
                int tagEnd = source.LastIndexOf(tagSuffix, index, StringComparison.Ordinal);

                if (tagEnd <= 0)
                    break;

                int tagStart = source.LastIndexOf(tagPrefix, tagEnd - 1, StringComparison.Ordinal);

                if (tagStart < 0)
                    break;

                // Find a key; use the case sensitivity specified by callers dictionary
                string key = source.Substring(tagStart + tagPrefix.Length, tagEnd - tagStart - tagPrefix.Length);

                if (dictionary.Contains(key))
                {
                    sb.Remove(tagStart, tagEnd + tagSuffix.Length - tagStart);
					sb.Insert(tagStart, (string)dictionary[key]);
                }
                else
                {
                    if ((flags & TaggedStringOptions.RemoveUnknownTags) == TaggedStringOptions.RemoveUnknownTags)
                    {
                        sb.Remove(tagStart, tagEnd + tagSuffix.Length - tagStart);
                    }
                    if ((flags & TaggedStringOptions.ThrowOnUnknownTags) == TaggedStringOptions.ThrowOnUnknownTags)
                    {
						throw new InvalidOperationException("{0}{1}{2} is undefined".CultureFormat(tagPrefix, key, tagSuffix));
                    }
                }
                
                index = tagStart - 1;
            }

            return sb.ToString();
        }
    }
}
