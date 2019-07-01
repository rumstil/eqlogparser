using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EQLogParser
{
    /// <summary>
    /// A resumable log file reader and watcher.
    /// </summary>
    public class LogReader : IDisposable    
    {
        public readonly string Path;
        public event Action<string> OnRead;
        readonly StreamReader Reader;
        readonly FileStream Stream;

        public LogReader(string path, int index = 0)
        {
            Path = path;
            Stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (index > 0 && Stream.Length > index)
                Stream.Position = index;
            if (index < 0 && Stream.Length > index)
                Stream.Position = Stream.Length + index;
            Reader = new StreamReader(Stream, Encoding.ASCII);
        }

        public LogReader(Stream stream)
        {
            Reader = new StreamReader(stream, Encoding.ASCII);
        }

        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// Close file handle.
        /// </summary>
        public void Close()
        {
            Reader.Close();
            Stream.Close();
        }

        public void Reset()
        {
            Stream.Position = 0;            
        }

        /// <summary>
        /// Read all lines in the file from the last place we left off and pass them to the OnRead callback.
        /// </summary>
        public void ReadAllLines()
        {
            if (OnRead == null)
                throw new InvalidOperationException("OnRead not initialized.");

            while (true)
            {
                var line = Reader.ReadLine();
                if (line == null)
                    return;
                OnRead(line);
            }
        }

        public void ReadAllLinesInParallel()
        {
            //Partitioner.Create()

        }

        public IEnumerable<string> Lines()
        {
            while (true)
            {
                var line = Reader.ReadLine();
                if (line == null)
                    yield break;
                yield return line;
            }
        }
    }
}
