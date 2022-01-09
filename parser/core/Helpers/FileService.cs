using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EQLogParser.Helpers
{
    public interface IFileService
    {
        bool Exists(string name);
        StreamReader OpenText(string name);
    }

    /// <summary>
    /// File system service that limits file access to a folder and it's children.
    /// This is intended for use in a server setting as a security measure.
    /// </summary>
    public class FileService : IFileService
    {
        public string Root { get; set; }

        public FileService(string root)
        {
            if (!Directory.Exists(root))
                throw new ArgumentException("Root folder not found.");
            Root = root;
            if (!Path.EndsInDirectorySeparator(Root))
                Root += Path.DirectorySeparatorChar;
        }

        public bool Exists(string name)
        {
            var path = Path.Combine(Root, Path.GetFileName(name));
            return File.Exists(path);
        }

        public StreamReader OpenText(string name)
        {
            var path = Path.Combine(Root, Path.GetFileName(name));
            return File.OpenText(path);
        }
    }

}