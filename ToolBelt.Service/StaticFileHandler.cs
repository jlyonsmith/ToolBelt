using ServiceStack;
using ServiceStack.Web;
using System;
using System.IO;
using System.Collections.Generic;
using ToolBelt;
using System.Reflection;
using ServiceStack.Host.Handlers;

namespace ToolBelt.Service
    {
    public class StaticFileHandler : HttpAsyncTaskHandler
    {
        static readonly Dictionary<string, string> extensionContentType;
        static readonly ParsedPath baseDirectory;

        ParsedPath path;

        static StaticFileHandler()
        {
            extensionContentType = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) 
            {
                { ".text", "text/plain" },
                { ".js", "text/javascript" },
                { ".css", "text/css" },
                { ".html", "text/html" },
                { ".htm", "text/html" },
                { ".png", "image/png" },
                { ".ico", "image/x-icon" },
                { ".gif", "image/gif" },
                { ".bmp", "image/bmp" },
                { ".jpg", "image/jpeg" }
            };

            baseDirectory = new ParsedPath(Assembly.GetEntryAssembly().Location, PathType.File).Directory;
        }

        protected StaticFileHandler(ParsedPath path) : base()
        {
            this.path = path;
        }

        public static StaticFileHandler Factory(string pathInfo)
        {
            if (!pathInfo.StartsWith("/"))
                return null;

            ParsedPath path;

            try
            {
                path = baseDirectory.Append(pathInfo.Substring(1), PathType.File);
            }
            catch (ArgumentException)
            {
                return null;
            }

            if (!File.Exists(path))
                return null;

            return new StaticFileHandler(path);
        }   

        public override void ProcessRequest(IRequest httpReq, IResponse httpRes, string operationName)
        {
            var bytes = File.ReadAllBytes(path);
            string contentType;

            httpRes.OutputStream.Write(bytes, 0, bytes.Length);
            httpRes.AddHeader("Date", DateTime.Now.ToString("R"));
            httpRes.AddHeader("Content-Type", extensionContentType.TryGetValue(path.Extension, out contentType) ? contentType : "text/plain");
        }
    }
}