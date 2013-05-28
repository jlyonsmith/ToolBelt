using System;

namespace ToolBelt
{
    public interface IHasTextFileRegion
    {
        TextFileRegion TextRegion { get; }
    }
    
    [Serializable]
    public class TextFileRegion
    {
        public TextFileRegion(TextFileMark start, TextFileMark end)
        {
            Start = start;
            End = end;
        }

        // TODO: Ensure that End is after Start
        public TextFileMark Start { get; set; }
        public TextFileMark End { get; set; }
    }
    
    [Serializable]
    public struct TextFileMark
    {
        public static TextFileMark Empty = new TextFileMark();

        private int line;
        private int column;
        private int index;

        public TextFileMark(int line, int column, int index)
        {
            this = Empty;
            Line = line;
            Column = column;
            Index = index;
        }

        public TextFileMark(TextFileMark mark)
        {
            this.line = mark.line;
            this.column = mark.column;
            this.index = mark.index;
        }

        public int Column
        {
            get
            {
                return this.column;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Column must be greater than or equal to zero.");
                }
                this.column = value;
            }
        }
        
        public int Index
        {
            get
            {
                return this.index;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Index must be greater than or equal to zero.");
                }
                this.index = value;
            }
        }
        
        public int Line
        {
            get
            {
                return this.line;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Line must be greater than or equal to zero.");
                }
                this.line = value;
            }
        }
        
        public override string ToString()
        {
            return string.Format("Lin:{0}, Col:{1}, Chr:{2}", this.line, this.column, this.index);
        }
    }
}

