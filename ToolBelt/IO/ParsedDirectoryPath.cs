using System;

namespace ToolBelt
{
    public sealed class ParsedDirectoryPath : ParsedPath
    {
        public ParsedDirectoryPath(string path) : base(path, PathType.Directory)
        {
        }

        public ParsedDirectoryPath(ParsedPath path) : base(path, PathParts.All)
        {
            if (!path.HasDirectory)
                throw new InvalidCastException("Must have a directory");
        }
    }
}

