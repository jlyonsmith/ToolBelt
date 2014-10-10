using System;

namespace ToolBelt
{
    public class ParsedDirectoryPath : ParsedPath
    {
        public ParsedDirectoryPath(string path) : base(path, PathType.Directory)
        {
        }
    }
}

