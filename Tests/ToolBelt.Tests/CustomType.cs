using System;
using System.Collections.Generic;
using System.Text;

namespace ToolBelt.Tests
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
}

