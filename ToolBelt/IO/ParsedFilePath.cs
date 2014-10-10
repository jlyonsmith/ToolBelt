using System;

namespace ToolBelt
{
    public class ParsedFilePath : ParsedPath
    {
        public ParsedFilePath(string path) : base(path, PathType.File)
        {
        }
    }
}

