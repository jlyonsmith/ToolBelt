using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;

namespace ToolBelt
{
    public class DuplicateItemException : Exception
    {
        public DuplicateItemException()
        {
        }
    }

    public class ParsedPathList : IList<ParsedPath>
    {
        private List<ParsedPath> paths;

        public ParsedPathList()
        {
            paths = new List<ParsedPath>();
        }

        public ParsedPathList(string pathList, PathType pathType)
        {
            string[] splitPaths = pathList.Split(Path.PathSeparator);
            
            paths = new List<ParsedPath>();
            
            foreach (string splitPath in splitPaths)
                paths.Add(new ParsedPath(splitPath, pathType));
        }

        public ParsedPathList(IEnumerable<ParsedPath> otherPaths)
        {
            this.paths = new List<ParsedPath>();

            foreach (var path in otherPaths)
                this.paths.Add(path);
        }

        public ParsedPathList(IList<string> pathList, PathType pathType)
        {
            paths = new List<ParsedPath>();

            foreach (string path in pathList)
                paths.Add(new ParsedPath(path, pathType));
        }

        #region IList<ParsedPath> Members

        public int IndexOf(ParsedPath item)
        {
            for (int i = 0; i < paths.Count; i++)
            {
                if (paths[i] == item)
                    return i;
            }

            return -1;
        }

        public void Insert(int index, ParsedPath item)
        {
            // Ensure that the path is not already in the list
            if (IndexOf(item) != -1)
                throw new DuplicateItemException();

            paths.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            paths.RemoveAt(index);
        }

        public ParsedPath this[int index]
        {
            get
            {
                return paths[index];
            }
            set
            {
                if (Contains(value))
                    throw new DuplicateItemException();

                paths[index] = value;
            }
        }

        #endregion

        #region ICollection<ParsedPath> Members

        public void Add(ParsedPath item)
        {
            if (Contains(item))
                return;

            paths.Add(item);
        }

        public void Clear()
        {
            paths.Clear();
        }

        public bool Contains(ParsedPath item)
        {
            return (IndexOf(item) != -1);
        }

        public void CopyTo(ParsedPath[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return paths.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(ParsedPath item)
        {
            return paths.Remove(item);
        }

        #endregion

        #region IEnumerable<ParsedPath> Members

        public IEnumerator<ParsedPath> GetEnumerator()
        {
            return paths.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)paths).GetEnumerator();
        }

        #endregion

        public override string ToString()
        {
            List<string> stringPaths = new List<string>(paths.Count);

            foreach (ParsedPath path in paths)
                stringPaths.Add(path.ToString());

            return StringUtility.Join(";", stringPaths);
        }

        public static implicit operator string(ParsedPathList pathList)
        {
            return pathList.ToString();
        }
    }
}
