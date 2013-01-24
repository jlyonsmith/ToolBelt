using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace ToolBelt
{
    /// <summary>
    /// Parsing hint for paths supplied to <see cref="ParsedPath"/> constructor.
    /// </summary>
    public enum PathType
    {
        /// <summary>
        /// The path is a file which may include a drive or share.
        /// </summary>
        File,
        /// <summary>
        /// The path is a directory which may include a drive or share.
        /// </summary>
        Directory,
        /// <summary>
        /// The path is just a Windows style "volume" (drive or share)
        /// </summary>
        Volume,
		/// <summary>
		/// A file or directory with wildcards
		/// </summary>
		Wildcard,
        /// <summary>
        /// The type of path is unknown.  This.  Will assume path is a directory if 
        /// it has a trailing <see cref="Path.DirectorySeparatorChar"/>, or a wildcard
		/// if there is a wildcard in the file name.
        /// </summary>
        Unknown,
    };

    public enum PathParts
    {
        Machine,
        Share,
        Drive,
        Directory,
        File,
        Extension,
        Volume,
        Unc,
        VolumeAndDirectory,
        VolumeDirectoryAndFile,
        DirectoryAndFile,
        DirectoryFileAndExtension,
        FileAndExtension,
        All,
    }

    /// <summary>
    /// A parses file system path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Most of the time it is convenient to work with a file system path as a simple string.  However
    /// this can be a problem if the origin of the string is unknown, i.e. provided from user input, as 
    /// significant processing is required in order to ensure that a passed in string is syntactically
    /// correct.  Additionally it is often necessary to extract one or more components of the path for a 
    /// specific purpose.  This class makes both of these tasks significantly easier and more efficient 
    /// than working with the path as a string.  
    /// </para>  
    /// <para>Parsing a file system path correctly is a tricky task. Paths can contain 
    /// Universal Naming Convention (UNC) components, they can contain relative directories, they
    /// might contain invalid characters, they can be quoted, they can contain wildcard characters, 
    /// they may refer to files, directories, or just a drive or UNC share. This class can handle all these 
    /// path components.</para>
    /// <para>This path also extends the normal relative path syntax by allowing more than two 
    /// <code>.</code>'s to signify parents directories further up the directory tree.  This is perhaps syntactically
    /// neater that using the <code>..\..</code> syntax.</para>
    /// <para>
    /// The path undergoes the following fix-ups, checks and formatting: 
    /// <list typeHint="bullet">
    /// <item>A check is made that the path is not null</item>
    /// <item>The path is unquoted ("") if necessary</item>
    /// <item>Leading and trailing whitespace is trimmed, both before and after unquoting</item>
    /// <item>A check is made that the path is > 1 character long</item>
	/// <item>A check is made that the path does not contain any invalid path characters</item>
	/// <item>Double directory separators are changed to a single separator.</item>
	/// <item>The <see cref="P:Directory"/> property is always given a terminating separator</item>
    /// <item><see cref="P:Drive"/> property will always have a drive letter separator character if it refers to a drive</item>
    /// <item>Extensions will always have a preceding <see cref="PathUtility.ExtensionSeparatorChar"/> character</item>
    /// <item>Trailing '.' and ' ' combinations are trimmed from all file and directory names</item>
    /// </list>
    /// </para>
    /// <para>
    /// This checking and formatting assures that properties representing concatenated sub-sections of the path, such as 
    /// <see cref="P:FileAndExtension"/> are always well formed. 
    /// </para>
    /// </remarks>
#if WINDOWS
    [Serializable]
#endif
    public class ParsedPath : IComparable
    {
        #region Fields

        private string path;
        private short machineLength;
        private short shareLength;
        private short driveLength;
        private short dirLength;
        private short fileLength;
        private short extLength;

        #endregion

        #region Class Data
        
        /// <summary>
        /// Represents an empty, uninitialized <see cref="ParsedPath"/>
        /// </summary>
        public static readonly ParsedPath Empty = new ParsedPath();
        
        /// <summary>
        /// Returns a regular expression that searches for surrounding quotes for a string on a single line
        /// </summary>
        public static Regex SurroundingQuotesSingleLineRegex
        {
            get
            {
                if (surroundingQuotesSingleLineRegex == null)
                    surroundingQuotesSingleLineRegex = new Regex("\"([^\"]*)\"", RegexOptions.Singleline);
                
                return surroundingQuotesSingleLineRegex;
            }
        }
        
        private static Regex surroundingQuotesSingleLineRegex = null;
        
        /// <summary>
        /// Returns a regular expression that searches a relatative compononent of a path, e.g. .\, ..\, ...\ 
        /// </summary>
        public static Regex RelativePathPartSingleLineRegex
        {
            get
            {
                if (relativePathPartSingleLineRegex == null)
                {
#if WINDOWS
                    relativePathPartSingleLineRegex = new Regex(@"[.]+\\");
#else
                    relativePathPartSingleLineRegex = new Regex(@"[.]+/");
#endif
                }
                
                return relativePathPartSingleLineRegex;
            }
        }
        
        private static Regex relativePathPartSingleLineRegex = null;
        
        #endregion

        #region Instance Constructors
        private ParsedPath()
        {
        }

        /// <summary>
        /// Initializes an instance of the <see cref="T:ParsedPath"/> class by parsing the given file 
        /// system path into its constituent parts.
        /// </summary>
        /// <exception cref="T:ArgumentException">The path could not be parsed.  The exception contains
        /// a description of the problem.</exception>
        /// <param name="path">The file path to parse.</param>
        /// <param name="typeHint">See <see cref="T:PathType"/> for a list of path type hints</param>
        public ParsedPath(string path, PathType typeHint)
        {
            string machine = String.Empty;
            string share = String.Empty;
            string drive = String.Empty;
            string dir = String.Empty;
            string file = String.Empty;
            string ext = String.Empty;

			ValidateAndNormalizePath(ref path);

            int i; // Always the beginning index
            int j; // Always the ending index

            bool autoTypeHint = (typeHint == PathType.Automatic);

            if (path.StartsWith(PathUtility.UncPrefixChars, StringComparison.InvariantCultureIgnoreCase))
            {
                i = 0;
                
                if (i + PathUtility.UncPrefixChars.Length >= path.Length)
                    throw new ArgumentException("Badly formed UNC name");
                    
                // Find the '\' after the '\\'
                j = path.IndexOf(Path.DirectorySeparatorChar, PathUtility.UncPrefixChars.Length);			
                
				if (j == -1 || j - i == 0)
					throw new ArgumentException("Badly formed UNC name");
                    
				machine = path.Substring(0, j);

				i = j;
                
				// Find the '\' after the share name, if there is one and it's not bang up
				// against the next one
				if (i + 1 >= path.Length || path[i + 1] == Path.DirectorySeparatorChar)
					throw new ArgumentException("Badly formed UNC name");
                
				j = path.IndexOf(Path.DirectorySeparatorChar, i + 1);

				// Either it wasn't found or the string ended
				if (j == -1)
					j = path.Length;
                
				if (j - i == 0)
					throw new ArgumentException("Badly formed UNC name");

				share = path.Substring(i, j - i);

				if (typeHint == PathType.Unknown && path.Length == machine.Length + share.Length)
					typeHint = PathType.Volume;
			}
			else if (path.Length >= 2 && path[1] == PathUtility.VolumeSeparatorChar)
			{
				drive = path.Substring(0, 2);
                
				if (typeHint == PathType.Unknown && path.Length == 2)
					typeHint = PathType.Volume;
			}

			if (typeHint == PathType.Unknown)
			{
				if (path[path.Length - 1] == Path.DirectorySeparatorChar)
				{
					typeHint = PathType.Directory;
				}
				else
				{			
					typeHint = PathType.File;
				}
			}
			else if (typeHint == PathType.Wildcard)
			{
				typeHint = PathType.File;
			}
            
            if (typeHint == PathType.File)	
            {
                // Get the file name
                i = path.LastIndexOfAny(
                    new char[2] {Path.DirectorySeparatorChar, Path.VolumeSeparatorChar}, path.Length - 1);
                
                // If we didn't find anything we must have an unrooted file
                if (i == -1)
                {
                    i = 0;
                }
                else 	
                {
                    if (i + 1 >= path.Length)
                        throw new ArgumentException("Path is badly formed.  It does not contain a file name.");

                    // Move past the separator; that's part of the directory
                    i++;
                }

                // There is a file name so we can continue.  Now that we know that, we can trim the end of 
                // the path.
                path = path.TrimEnd(PathUtility.BadDirTrailChars);
                
                // Chop off the file name
                file = path.Substring(i, path.Length - i);

                j = file.LastIndexOf(PathUtility.ExtensionSeparatorChar);

                if (j != -1 && j + 1 < file.Length)
                {
                    // Chop off the extension which is everything from the last '.' in the file name (if any)
                    ext = file.Substring(j);
                    file = file.Substring(0, j);
                }
            }
                
            // Get the directory - it's everything that's left (which may be nothing)
            dir = path.Substring(machine.Length + share.Length + drive.Length, 
                path.Length - machine.Length - share.Length - drive.Length - file.Length - ext.Length);

            // You have got to have at least a directory if the type hint is to expect a directory
            if (typeHint == PathType.Directory && dir == String.Empty)
                throw new ArgumentException("Missing directory");

            // Fix double directory separators - it happens too often to be an error
            dir = dir.Replace(PathUtility.UncPrefix, String.Empty + Path.DirectorySeparatorChar);

            // You can't have wildcards in the directory part
            if (dir.IndexOfAny(PathUtility.WildcardChars) != -1)
                throw new ArgumentException("Invalid characters in path. Directory cannot contain wildcards.");
            
            // If user wanted a VolumeOnly, validate that we have no directory, file or extension
            if (typeHint == PathType.Volume && 
                (dir != String.Empty || file != String.Empty || ext != String.Empty))
                throw new ArgumentException("Path contains more than just a volume");

            // Now we go through the directory and remove trailing space/dot combinations
            string[] dirParts = dir.Split(new char[] {Path.DirectorySeparatorChar});
            StringBuilder sb = new StringBuilder(dir.Length);
            
            for (i = 0; i < dirParts.Length; i++)
            {
                string dirPart = dirParts[i];
            
                // An empty dirPart is either a leading or trailing '\'
                if (dirPart == "")
                {
                    // Don't add the trailing on back in as it's already been done in the last iteration
                    if (i != dirParts.Length - 1)
                        sb.Append(Path.DirectorySeparatorChar);
                    
                    continue;
                }
                
                // Remove trailing spaces
                dirPart = dirPart.TrimEnd();

                // Deals with '\   \' situation
                if (dirPart == "")
                    throw new ArgumentException("Empty directory name");
                    
                // Only trim remaining dots and spaces if the directory is not all dots (relative)  
                for (j = dirPart.Length - 1; j >= 0; j--)
                    if (dirPart[j] != PathUtility.ExtensionSeparatorChar)
                        break;
                
                if (j > 0)
                {
                    dirPart = dirPart.TrimEnd(PathUtility.BadDirTrailChars);
                    
                    // Deals with the '\.. . . ..   .....\' situation
                    if (dirPart == "")
                        throw new ArgumentException("Empty directory name");
                }
                
                sb.Append(dirPart);
                sb.Append(Path.DirectorySeparatorChar);
            }
            
            dir = sb.ToString();

            this.path = ((machine != String.Empty) ? machine + share : drive) + dir + file + ext;
            this.machineLength = checked((short)machine.Length);
            this.shareLength = checked((short)share.Length);
            this.driveLength = checked((short)drive.Length);
            this.dirLength = checked((short)dir.Length);
            this.fileLength = checked((short)file.Length);
            this.extLength = checked((short)ext.Length);
        }

        /// <summary>
        /// Initializes a <see cref="ParsedPath"/> from another <see cref="ParsedPath"/> which avoids the need to reparse the path.
        /// </summary>
        /// <param name="path">The source path</param>
        /// <param name="parts">The parts to copy from the source path</param>
        public ParsedPath(ParsedPath path, PathParts parts)
        {
            switch (parts)
            {
                case PathParts.Machine:
                    this.machineLength = path.machineLength;
                    this.path = path.Machine;
                    break;
                case PathParts.Share:
                    this.shareLength = path.shareLength;
                    this.path = path.Share;
                    break;

                case PathParts.Drive:
                    this.driveLength = path.driveLength;
                    this.path = path.Drive;
                    break;

                case PathParts.Directory:
                    this.dirLength = path.dirLength;
                    this.path = path.Directory;
                    break;

                case PathParts.File:
                    this.fileLength = path.fileLength;
                    this.path = path.File;
                    break;

                case PathParts.Extension:
                    this.extLength = path.extLength;
                    this.path = path.Extension;
                    break;

                case PathParts.Volume:
                    if (path.HasDrive)
                    {
                        this.driveLength = path.driveLength;
                        this.path = path.Drive;
                    }
                    else
                    {
                        this.machineLength = path.machineLength;
                        this.shareLength = path.shareLength;
                        this.path = path.Machine + path.Share;
                    }
                    break;

                case PathParts.Unc:
                    this.machineLength = path.machineLength;
                    this.shareLength = path.shareLength;
                    this.path = path.Machine + path.Share;
                    break;

                case PathParts.VolumeAndDirectory:
                    if (path.HasDrive)
                    {
                        this.driveLength = path.driveLength;
                        this.dirLength = path.dirLength;
                        this.path = path.Drive + path.Directory;
                    }
                    else
                    {
                        this.machineLength = path.machineLength;
                        this.shareLength = path.shareLength;
                        this.dirLength = path.dirLength;
                        this.path = path.Machine + path.Share + path.Directory;
                    }
                    break;

                case PathParts.VolumeDirectoryAndFile:
                    if (path.HasDrive)
                    {
                        this.driveLength = path.driveLength;
                        this.dirLength = path.dirLength;
                        this.fileLength = path.fileLength;
                        this.path = path.Drive + path.Directory + path.File;
                    }
                    else
                    {
                        this.machineLength = path.machineLength;
                        this.shareLength = path.shareLength;
                        this.dirLength = path.dirLength;
                        this.fileLength = path.fileLength;
                        this.path = path.Machine + path.Share + path.Directory + path.File;
                    }
                    break;

                case PathParts.DirectoryAndFile:
                    this.dirLength = path.dirLength;
                    this.fileLength = path.fileLength;
                    this.path = path.Directory + path.File;
                    break;

                case PathParts.DirectoryFileAndExtension:
                    this.dirLength = path.dirLength;
                    this.fileLength = path.fileLength;
                    this.extLength = path.extLength;
                    this.path = path.Directory + path.File + path.Extension;
                    break;

                case PathParts.FileAndExtension:
                    this.fileLength = path.fileLength;
                    this.extLength = path.extLength;
                    this.path = path.File + path.Extension;
                    break;

                case PathParts.All:
                    if (path.HasDrive)
                    {
                        this.driveLength = path.driveLength;
                        this.dirLength = path.dirLength;
                        this.fileLength = path.fileLength;
                        this.extLength = path.extLength;
                        this.path = path.Drive + path.Directory + path.File + path.Extension;
                    }
                    else
                    {
                        this.machineLength = path.machineLength;
                        this.shareLength = path.shareLength;
                        this.dirLength = path.dirLength;
                        this.fileLength = path.fileLength;
                        this.extLength = path.extLength;
                        this.path = path.Machine + path.Share + path.Directory + path.File + path.Extension;
                    }
                    break;
            }
        }

        private ParsedPath(
            string path, 
            int machineLength, 
            int shareLength, 
            int driveLength, 
            int dirLength, 
            int fileLength, 
            int extLength)
        {
            this.path = path;
            this.machineLength = checked((short)machineLength);
            this.shareLength = checked((short)shareLength);
            this.driveLength = checked((short)driveLength);
            this.dirLength = checked((short)dirLength);
            this.fileLength = checked((short)fileLength);
            this.extLength = checked((short)extLength);
        }

        #endregion

        #region Instance Methods

		private void ValidateAndNormalizePath(ref string path)
		{
			// Null reference is bad
			if (path == null)
				throw new ArgumentNullException("path");
			
			// Remove leading/trailing spaces 
			path = path.Trim();
			
			// Remove any surrounding quotes
			if (path.Length >= 2 && path[0] == '\"')
			{
				// TODO: This can be done without a RegEx for better performance
				path = SurroundingQuotesSingleLineRegex.Replace(path, @"$1");
				path = path.Trim(null);
			}
			
			// Do we still have anything?
			if (path.Length == 0)
				throw new ArgumentException("Path is zero length");
			
			// Do an invalid character check once now
			if (path.IndexOfAny(PathUtility.InvalidPathChars) != -1)
			{
				throw new ArgumentException("Path contains invalid characters");
			}
			
			// Convert '/' into '\' or vica versa
			path = path.Replace(PathUtility.AltDirectorySeparatorChar, PathUtility.DirectorySeparatorChar);
		}

        /// <summary>
        /// Combine a path fragment with an existing path. You can only combine to the path if it is not 
        /// contain a filename or extension.
        /// </summary>
        /// <param name="path">A path that is a directory fragment and/or a filename</param>
        /// <param name="pathFragmentType">The type of the passed in path fragment</param>
        /// <returns></returns>
        public ParsedPath Append(string pathFragment, PathType pathFragmentType)
        {
            return this.Append(new ParsedPath(pathFragment, pathFragmentType));
        }

        /// <summary>
        /// Combine a path fragment with an existing path. You can only combine to the path if it does not 
        /// contain a filename or extension and the path fragment has no root directory or volume.
        /// </summary>
        /// <param name="path">A path that is a directory fragment and/or a filename</param>
        /// <returns></returns>
        public ParsedPath Append(ParsedPath pathFragment)
        {
            if (this.HasFilename)
                throw new ArgumentException("Target path already has a file name and/or extension");

            if (pathFragment.HasVolume)
                throw new ArgumentException("Path fragment must not contain a volume");

            if (pathFragment.HasRootDirectory)
                throw new ArgumentException("Path fragment must not have a root directory");

            string dir = this.Directory.ToString() + pathFragment.Directory.ToString();
            string path = this.Volume + dir + pathFragment.File + pathFragment.Extension;

            return new ParsedPath(
                path, 
                machineLength, 
                shareLength, 
                driveLength, 
                dir.Length, 
                pathFragment.fileLength, 
                pathFragment.extLength);
        }

        /// <summary>
        /// Create a <see cref="ParsedPath"/> with a different extension
        /// </summary>
        /// <param name="newExtension"></param>
        /// <returns></returns>
        public ParsedPath WithExtension(string newExtension)
        {
			ValidateAndNormalizePath(ref newExtension);

            if (!newExtension.StartsWith("."))
                throw new ArgumentException("Extensions must start with a '.'");

            string path = this.Volume + this.Directory + this.File + newExtension;

            return new ParsedPath(
                path, 
                machineLength, 
                shareLength, 
                driveLength, 
                dirLength, 
                fileLength, 
                newExtension.Length);
        }

        /// <summary>
        /// Set the file name to the give string.
        /// </summary>
        /// <param name="newFile">The new file name</param>
        /// <returns>A new path with the give file name</returns>
        public ParsedPath WithFileAndExtension(string newFileAndExtension)
        {
			ValidateAndNormalizePath(ref newFileAndExtension);

            string newFile;
            string newExtension;

            int n = newFileAndExtension.LastIndexOf(PathUtility.ExtensionSeparatorChar);

            if (n == -1)
            {
                newExtension = String.Empty;
                newFile = newFileAndExtension;
            }
            else
            {
                newExtension = newFileAndExtension.Substring(n, newFileAndExtension.Length - n);
                newFile = newFileAndExtension.Substring(0, n);

                if (newFile.Length == 0)
                    throw new ArgumentException("Only an extension was given");
            }

            string path = this.Volume + this.Directory + newFile + newExtension;

            return new ParsedPath(
                path,
                machineLength,
                shareLength,
                driveLength,
                dirLength,
                newFile.Length,
                newExtension.Length);
        }

		/// <summary>
		/// Creates a new <see cref="ParsedPath"/> with the new directory.
		/// </summary>
		/// <returns>
		/// The new <see cref="ParsedPath"/>.
		/// </returns>
		/// <param name='newDirectory'>
		/// New directory.
		/// </param>
		public ParsedPath WithDirectory(IEnumerable<ParsedPath> newDirectoryParts)
		{
			StringBuilder sb = new StringBuilder();

			foreach (var newDirectoryPart in newDirectoryParts)
			{
				sb.Append(newDirectoryPart.Directory.ToString());
			}
	
			string newDir = sb.ToString();
			string path = this.Volume + newDir + this.File + this.Extension;

			return new ParsedPath(
				path,
				machineLength,
				shareLength,
				driveLength,
				newDir.Length,
				fileLength,
				extLength);
		}

		public ParsedPath WithDirectory(ParsedPath directory)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a new ParsedPath with the new volume.
		/// </summary>
		/// <returns>
		/// The volume.
		/// </returns>
		/// <param name='newVolume'>
		/// New volume.
		/// </param>
		public ParsedPath WithVolume(string newVolume)
		{
			throw new NotImplementedException();
		}

        /// <overloads>Creates a fully qualified file path.</overloads>
        /// <summary>
        /// Creates a fully qualified file path from a partial path and the current directory.
        /// </summary>
        /// <remarks>
        /// This method is equivalent to <see cref="T:MakeFullPath(System.String)"/> 
        /// with the applications current directory as the base path.
        /// </remarks>
        /// <returns>The fully qualified file path</returns>
        public ParsedPath MakeFullPath()
        {
            return MakeFullPath(null);
        }
        
        /// <summary>
        /// Makes the <see cref="ParsedPath"/> fully qualified using the specified base path. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="ParsedPath"/> may have a missing volume and contain relative directories.  A path volume is a drive letter or machine/share.
        /// This method returns a path containing a volume name, root directory and no relative directory parts.  If the <see cref="ParsedPath"/>  
        /// does not contain a drive or machine/share the function begins by prefixing the <see cref="ParsedPath"/> with the base path. 
        /// </para>
        /// <para>
        /// In addition to the standard '.' and '..' relative directories, you can specify three or more periods to represent
        /// increasing levels of parent directory. 
        /// </para>
        /// <para>The function does not check to see if the resulting path points to valid file or directory.</para> 
        /// </remarks>
        /// <param name="basePath">The base path to use.  It must have a volume and directory parts.  Any file or extension is ignored.</param>
        /// <exception cref="System.ArgumentException">
        /// The path contains too many parent directories, such that the path would traverse beyond the volume, 
        /// or the path contains invalid file name characters.
        /// </exception>
        /// <returns>The fully qualified file system path.</returns>
        public ParsedPath MakeFullPath(ParsedPath basePath)
        {
            if (basePath == null)
                basePath = new ParsedPath(System.Environment.CurrentDirectory, PathType.Directory);

#if WINDOWS
			if (!basePath.HasVolume)
				throw new ArgumentException("Base directory has no volume");
#else
			if (basePath.HasVolume)
				throw new ArgumentException("Base directory is not a Unix style path");
#endif

            if (basePath.Directory == String.Empty)
                throw new ArgumentException("Base directory has no directory");

			if (!basePath.HasRootDirectory)
				throw new ArgumentException("Base directory has no root");
                
            string machine = String.Empty;
            string share = String.Empty;
            string drive = String.Empty;
            string dir = this.Directory;
            string file = this.File;
            string ext = this.Extension;
        
            // Does this path contain a volume?
            if (this.HasVolume)
            {
                // Yes, copy this paths volume
                if (HasUnc)
                {
                    machine = this.Machine;
                    share = this.Share;	
                }
                else
                {
                    drive = this.Drive;
                }
            }
            else
            {
                // No, copy base path volume
                if (basePath.HasUnc)
                {
                    machine = basePath.Machine;
                    share = basePath.Share;	
                }
                else
                {
                    drive = basePath.Drive;
                }
            }

			#if OSX
			if (dir.StartsWith("~/"))
			{
				ParsedPath personalPath = new ParsedPath(Environment.GetFolderPath(Environment.SpecialFolder.Personal), PathType.Directory);

				dir = personalPath + dir.Substring(2);
			}
			#endif

            StringBuilder sb = new StringBuilder(dir.Length);
            int index = 0;  
            // This is the index of the first character of the remainder of the unprocessed part of the directory

            // Does this path contain a rooted directory, i.e. does it start with '\'?
            if (dir.Length > 0 && dir[index] == Path.DirectorySeparatorChar)
            {	
                // Yes, start with the volume directory
                sb.Append(dir[index++]);
            }
            else
            {
                // No, start the new directory with the base path directory
                sb.Append(basePath.Directory);
            }

            // Find the offset of any relative part of this path
            Match m = RelativePathPartSingleLineRegex.Match(dir);
                
            while (m.Success)
            {
                // Move past the non-relative portion
                sb.Append(dir, index, m.Index - index);
                index = m.Index;
            
                // Count the dots
                int dotCount = 0;
                
                for (; dir[index] == PathUtility.ExtensionSeparatorChar; index++, dotCount++);
                
                // Jump over the '\'
                index++;
                
                // Only dot counts greater than one have any effect
                dotCount--;
                
                for (; dotCount > 0; dotCount--)
                {
                    // Find the start of the last directory
                    int i = sb.Length - 2;
                    
                    for (; i >= 0 && sb[i] != Path.DirectorySeparatorChar; i--);
                    
                    if (i < 0)
                        throw new ArgumentException("Too many relative directories in path");

                    sb.Remove(i + 1, sb.Length - i - 1);
                }
                
                m = m.NextMatch();
            }
        
            // Add any trailing non-relative part
            sb.Append(dir, index, dir.Length - index);
            
            dir = sb.ToString();

            string path = (machine.Length == 0 ? drive : machine + share) + dir + file + ext;

            return new ParsedPath(
                path, machine.Length, share.Length, drive.Length, dir.Length, file.Length, ext.Length);
        }

        /// <summary>
        /// Makes a new path which is this path made relative to a base path.  The <see cref="ParsedPath"/> can 
        /// only be made relative if <see cref="IsFull"/> returns <code>true</code> for this path and the base path,
        /// and the <see cref="ParsedPath.Volume"/> must be the same for both paths.
        /// Only the directory portion of the new path will differ from the original path.
        /// </summary>
        /// <param name="basePath">The new path will be relative to the directory of this path</param>
        /// <returns>This path made relative to the give base path</returns>
        public ParsedPath MakeRelativePath(ParsedPath basePath)
        {
            if (!this.IsFullPath && !basePath.IsFullPath)
                throw new ArgumentException("Both paths must be fully qualified");
                
            if (this.Volume != basePath.Volume)
                throw new ArgumentException("Both paths must have the same volume (drive or UNC name)");

            string machine = this.Machine;
            string share = this.Share;
            string drive = this.Drive;
            string file = this.File;
            string ext = this.Extension;
            
            // Get the directory portions of both paths as arrays
            IList<ParsedPath> dirs = this.SubDirectories;
            IList<ParsedPath> baseDirs = basePath.SubDirectories;
            int min = Math.Min(dirs.Count, baseDirs.Count);
            
            // Skip over any directory names that are the same
            int n = 0;
            
            while (n < min)
            {
                if (String.Compare(dirs[n], baseDirs[n], StringComparison.InvariantCultureIgnoreCase) != 0)
                    break;
                    
                n++;
            }

            StringBuilder sb = new StringBuilder(dirLength);

            // If there is nothing left of the base directory, add a '.' then the rest of the directories in this path
            if (baseDirs.Count - n == 0)
            {
                sb.Append(PathUtility.ExtensionSeparatorChar);
                sb.Append(Path.DirectorySeparatorChar);
                
                for (; n < dirs.Count; n++)
                {
                    sb.Append(dirs[n]);
                    sb.Append(Path.DirectorySeparatorChar);
                }
            }
            else
            {
                // Otherwise add ..'s for number of remaining directories in base then the rest of the directories in this path
                int m = n;
                
                for (; n < baseDirs.Count; n++)
                {
                    sb.Append(PathUtility.ExtensionSeparatorChar);
                    sb.Append(PathUtility.ExtensionSeparatorChar);
                    sb.Append(Path.DirectorySeparatorChar);
                }

                for (n = m; n < dirs.Count; n++)
                {
                    sb.Append(dirs[n]);
                    sb.Append(Path.DirectorySeparatorChar);
                }
            }
            
            string dir = sb.ToString();

            string path = (machine.Length == 0 ? drive : machine + share) + dir + file + ext;
            
            return new ParsedPath(
                path,
                machine.Length,
                share.Length,
                drive.Length,
                dir.Length,
                file.Length,
                ext.Length);
        }

		/// <summary>
		/// Makes the parent path.
		/// </summary>
		/// <returns>
		/// The parent path.
		/// </returns>
		/// <param name='level'>
		/// Number of levels up the path to go; 0, -1, -2, etc..
		/// </param>
		public ParsedPath MakeParentPath(int level)
		{
			IList<ParsedPath> subDirs = this.SubDirectories;
			int n = subDirs.Count + level;

			if (n == 0)
				throw new InvalidOperationException(
					"Insufficient directories to make parent path {0} levels down".CultureFormat(-level));

			return this.WithDirectory(new ListRange<ParsedPath>(subDirs, 0, n));
		}

		public ParsedPath MakeParentPath()
		{
			return MakeParentPath(-1);
		}
        
        #endregion

        #region Static Methods
        /// <summary>
        /// Parse a string and return a <see cref="ParsedPath"/> object.  Use <see cref="PathType.Automatic"/> to determine path type.
        /// </summary>
        /// <param name="value">String value to parse.</param>
        /// <returns></returns>
        public static ParsedPath Parse(string value)
        {
            return new ParsedPath(value, PathType.Unknown);
        }

        #endregion

        #region Overrides
        /// <summary>
        /// Returns a <see cref="T:System.String"/> equivalent to the entire path 
        /// </summary>
        public override string ToString()
        {
            return path;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            ParsedPath path = obj as ParsedPath;
            
            if (path == null)
                return false;

            return this.path.Equals(path.path, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Returns <code>true</code> if two <see cref="ParsedPath"/> objects are equal.
        /// </summary>
        /// <param name="pp1"></param>
        /// <param name="pp2"></param>
        /// <returns></returns>
        public static bool Equals(ParsedPath pp1, ParsedPath pp2)
        {
            if ((object)pp1 == (object)pp2)
                return true;
                
            if ((object)pp1 == null || (object)pp2 == null)
                return false;
                
            return pp1.Equals(pp2);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
			// TODO: This needs to be case insensitive on Windows!
            return this.ToString().GetHashCode();
        }

        /// <summary>
        /// Compares two parsed paths for equivalence.
        /// </summary>
        /// <remarks>This method uses <see cref="String.Compare(string, string, bool)"/> to compare the two paths</remarks>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static int Compare(ParsedPath a, ParsedPath b)
        {
            return String.Compare(a.ToString(), b.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Compare this to another for equivalence.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;


            if (obj is ParsedPath)
            {
                return String.Compare(this.ToString(), ((ParsedPath)obj).ToString(), StringComparison.InvariantCultureIgnoreCase);
            }
            else if (obj is String)
            {
                return String.Compare(this.ToString(), ((String)obj), StringComparison.InvariantCultureIgnoreCase);
            }
            else
            {
                throw new ArgumentException("Object is not a ParsedPath or String");
            }
        }

        #endregion

        #region Operators and Conversions
        /// <summary>
        /// Implicit conversion to a string
        /// </summary>
        /// <param name="path">The parsed path to convert</param>
        /// <returns>A string containing equivalent to the entire path</returns>
        public static implicit operator String(ParsedPath path)
        {
            if (path == null)
                return null;
                
            return path.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pp1"></param>
        /// <param name="pp2"></param>
        /// <returns></returns>
        public static bool operator ==(ParsedPath pp1, ParsedPath pp2)
        {
            return Equals(pp1, pp2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pp1"></param>
        /// <param name="pp2"></param>
        /// <returns></returns>
        public static bool operator !=(ParsedPath pp1, ParsedPath pp2)
        {
            return !Equals(pp1, pp2);
        }

        #endregion

        #region Instance Properties
        /// <summary>
        /// Gets a boolean value which indicates whether the path has UNC volume
        /// </summary>
        public bool HasUnc
        {
            get 
            {
                // If machine is set, share must be by definition
                return (machineLength != 0);
            }
        }
            
        /// <summary>
        /// Gets a boolean value which indicates whether the path has a drive volume
        /// </summary>
        public bool HasDrive
        {
            get 
            {
                // If machine is set, share must be by definition
                return (driveLength != 0);
            }
        }

        /// <summary>
        /// Gets a boolean value indicating whether the path has a directory
        /// </summary>
        public bool HasDirectory
        {
            get
            {
                return (dirLength != 0);
            }
        }

        /// <summary>
        /// Gets a boolean value indicating whether the path has a file name
        /// </summary>
        public bool HasFilename
        {
            get
            {
                return (fileLength != 0);
            }
        }

        /// <summary>
        /// Gets a boolean value indicating whether the path has an extension
        /// </summary>
        public bool HasExtension
        {
            get
            {
                return (extLength != 0);
            }
        }

        /// <summary>
        /// Gets a boolean value which indicates the path file name contains wildcard characters '*' or '?'.
        /// </summary>
        public bool HasWildcards
        {
            get
            {
                return (FileAndExtension.ToString().IndexOfAny(PathUtility.WildcardChars) != -1);
            }
        }

        /// <summary>
        /// Gets a boolean value which indicates whether the path has a volume
        /// </summary>
        public bool HasVolume
        {
            get 
            {
                return HasDrive || HasUnc;
            }
        }

        /// <summary>
        /// Gets a boolean indicating whether the path has a root directory or not.
        /// </summary>
        public bool HasRootDirectory
        {
            get
            {
                return dirLength > 0 && path[machineLength + shareLength + driveLength] == Path.DirectorySeparatorChar;
            }
        }

        /// <summary>
        /// Gets a boolean value which indicates whether the directory part refers to the root directory.
        /// <code>false</code> is returned if there is no directory part.
        /// </summary>
        public bool IsRootDirectory
        {
            get 
            {
                return (dirLength == 1 && path[machineLength + shareLength + driveLength] == Path.DirectorySeparatorChar);
            }
        }

        /// <summary>
        /// Gets a boolean value which indicates if the path is a only a volume, i.e. it contains no directory, filename
        /// or extension.
        /// </summary>
        public bool IsVolume
        {
            get
            {
                return HasVolume && !HasDirectory && !HasFilename && !HasExtension;
            }
        }

        /// <summary>
        /// Gets a boolean value which indicates if the path is a directory, i.e. it contains no filename and no extension.
        /// </summary>
        public bool IsDirectory
        {
            get
            {
                return (!HasFilename && !HasExtension);
            }
        }

        /// <summary>
        /// Gets a boolean value which indicates if the path is a directory only, i.e. it contains no volume, filename
        /// or extension.
        /// </summary>
        public bool IsDirectoryOnly
        {
            get 
            {
                return (!HasVolume && !HasFilename && !HasExtension);
            }
        }

        /// <summary>
        /// Gets a boolean value which indicates if the path is a filename, i.e. it contains a filename and/or extension
        /// </summary>
        public bool IsFilename
        {
            get
            {
                return (HasFilename || HasExtension);
            }
        }

        /// <summary>
        /// Gets a boolean value which indicates if the path is a filename only, i.e. it contains no volume or directory.
        /// </summary>
        public bool IsFilenameOnly
        {
            get
            {
                return (!HasVolume && !HasDirectory);
            }
        }

        /// <summary>
        /// Gets a boolean value which indicates if the path contains a relative directory.
        /// </summary>
        public bool IsRelativePath
        {
            get 
            {
                return (!HasDirectory || !HasRootDirectory || RelativePathPartSingleLineRegex.Match(Directory).Success);
            }
        }
            
        /// <summary>
        /// Gets a boolean value which indicates if the path is fully qualified.
        /// </summary>
        public bool IsFullPath
        {
            get 
            {
                return (HasVolume && !IsRelativePath);
            }
        }
        
        /// <summary>
        /// Gets an array containing the sub-directories that make up the full directory name.
        /// </summary>
        public IList<ParsedPath> SubDirectories
        {
            get 
            {
				List<ParsedPath> subDirs = new List<ParsedPath>();

				subDirs.Add(new ParsedPath(Path.DirectorySeparatorChar.ToString(), PathType.Directory));

				string dir = this.Directory;
				int i = 1;

				for (int j = i; j < dir.Length;)
				{
					if (dir[j] != Path.DirectorySeparatorChar)
					{
						subDirs.Add(new ParsedPath(dir.Substring(i, j - i + 1), PathType.Directory));
						j++;
						i = j;
					}
					else
					{
						j++;
					}
				}

                return subDirs;
            }
        }
        
        /// <summary>
        /// Get the machine name.
        /// </summary>
        public string Machine 
        { 
            get 
            { 
                return path.Substring(0, machineLength); 
            } 
        }
        /// <summary>
        /// Get the share name.
        /// </summary>
        public string Share 
        { 
            get 
            { 
                return path.Substring(machineLength, shareLength); 
            } 
        }
        /// <summary>
        /// Get the drive name.  
        /// </summary>
        public ParsedPath Drive 
        { 
            get 
            { 
                return new ParsedPath(path.Substring(0, driveLength), 0, 0, driveLength, 0, 0, 0); 
            } 
        }
        /// <summary>
        /// Get the path volume which is one of either machine/share or drive.  
        /// </summary>
        public ParsedPath Volume 
        { 
            get 
            {
                return new ParsedPath(machineLength == 0 ? 
                    path.Substring(0, driveLength) : path.Substring(0, machineLength + shareLength), 
                    machineLength, shareLength, driveLength, 0, 0, 0);
            } 
        }
        /// <summary>
        ///	Get the directory part of the path.  
        /// </summary>
        public ParsedPath Directory 
        { 
            get 
            {
                return new ParsedPath(
                    path.Substring(machineLength + shareLength + driveLength, dirLength), 0, 0, 0, dirLength, 0, 0); 
            } 
        }
        /// <summary>
        ///	Get the name of the last sub-directory in the directory name, with no trailing separator
        /// </summary>
        public ParsedPath LastDirectory
        {
            get
            {
                if (dirLength > 0)
                {
                    if (IsRootDirectory)
                        return new ParsedPath(
                            path.Substring(machineLength + shareLength + driveLength, dirLength), 
                            0, 0, 0, dirLength, 0, 0);
                    else
                    {
                        int i = machineLength + shareLength + driveLength;
                        int j = path.LastIndexOf(Path.DirectorySeparatorChar, i + dirLength - 2) + 1;
                        int k = i + dirLength - j;

                        return new ParsedPath(path.Substring(j, k), 0, 0, 0, k, 0, 0);
                    }
                }
                else
                    return new ParsedPath();
            }
        }
        /// <summary>
        ///	Get the name of the last sub-directory in the directory name, with no trailing separator unless the directory is the root directory.
        /// </summary>
        public string LastDirectoryNoSeparator
        { 
            get 
            {
                if (IsRootDirectory)
                    return LastDirectory;
                else
                    return LastDirectory.ToString().TrimEnd(new char[] { Path.DirectorySeparatorChar });
            } 
        }
        /// <summary>
        ///	Get the directory part of the path.  
        /// </summary>
        public string DirectoryNoSeparator 
        { 
            get 
            { 
                return Directory.ToString().TrimEnd(new char[] {Path.DirectorySeparatorChar}); 
            } 
        }
        /// <summary>
        /// Get the file name part of the path.
        /// </summary>
        public ParsedPath File 
        { 
            get 
            { 
                return new ParsedPath(
                    path.Substring(machineLength + shareLength + driveLength + dirLength, fileLength), 
                    0, 0, 0, 0, fileLength, 0); 
            } 
        }
        /// <summary>
        /// Get the extension part of the path.  
        /// </summary>
        public ParsedPath Extension 
        { 
            get 
            { 
                return new ParsedPath(
                    path.Substring(machineLength + shareLength + driveLength + dirLength + fileLength, extLength),
                    0, 0, 0, 0, 0, extLength); 
            } 
        }
        /// <summary>
        /// Get the concatenated drive and directory.
        /// </summary>
        public ParsedPath VolumeAndDirectory 
        { 
            get 
            { 
                return new ParsedPath(path.Substring(0, machineLength + shareLength + driveLength + dirLength), 
                    machineLength, shareLength, driveLength, dirLength, 0, 0); 
            } 
        }
        /// <summary>
        /// Get the concatenated drive and directory.
        /// </summary>
        public string VolumeAndDirectoryNoSeparator 
        { 
            get 
            { 
                return VolumeAndDirectory.ToString().TrimEnd(new char[] {Path.DirectorySeparatorChar}); 
            } 
        }
        /// <summary>
        /// Get the concatenated directory and file.
        /// </summary>
        public ParsedPath DirectoryAndFile 
        { 
            get 
            { 
                return new ParsedPath(
                    path.Substring(machineLength + shareLength + driveLength, dirLength + fileLength), 
                    0, 0, 0, dirLength, fileLength, 0); 
            } 
        }
        /// <summary>
        /// Get the concatenated file name and extension.
        /// </summary>
        public ParsedPath FileAndExtension 
        { 
            get 
            { 
                return new ParsedPath(
                    path.Substring(machineLength + shareLength + driveLength + dirLength, fileLength + extLength),
                    0, 0, 0, 0, fileLength, extLength); 
            } 
        }
        /// <summary>
        /// Get the concatenated machine, share, directory and file.
        /// </summary>
        public ParsedPath VolumeDirectoryAndFile 
        { 
            get 
            {
                return new ParsedPath(
                    path.Substring(0, machineLength + shareLength + driveLength + dirLength + fileLength), 
                    machineLength, shareLength, driveLength, dirLength, fileLength, 0);
            } 
        }
        /// <summary>
        /// Get the concatenated directory, file and extension.
        /// </summary>
        public ParsedPath DirectoryFileAndExtension
        {
            get
            {
                return new ParsedPath(
                    path.Substring(machineLength + shareLength + driveLength, dirLength + fileLength + extLength),
                    0, 0, 0, dirLength, fileLength, extLength);
            }
        }

        #endregion
    }
}
