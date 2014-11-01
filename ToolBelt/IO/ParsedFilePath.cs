using System;

namespace ToolBelt
{
    public sealed class ParsedFilePath : ParsedPath
    {
        public ParsedFilePath(string path) : base(path, PathType.File)
        {
        }

        public ParsedFilePath(ParsedPath path) : base(path, PathParts.All)
        {
            if (!path.HasFilename)
                throw new InvalidCastException("Must have a file name");
        }
    }
}

