using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;


namespace EQLogParser
{
    public class LogReader : IDisposable    
    {
        public readonly string Path;
        private readonly StreamReader reader;
        private readonly FileStream stream;

        public LogReader(string path, int index = 0)
        {
            Path = path;

            stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            if (path.EndsWith(".gz"))
            {
                var gzip = new GZipStream(stream, CompressionMode.Decompress);
                reader = new StreamReader(gzip, Encoding.ASCII);
            }
            else
            {
                // seeking can only be used in an uncompressed stream
                if (index > 0 && stream.Length > index)
                    stream.Position = index;
                if (index < 0 && stream.Length > index)
                    stream.Position = stream.Length + index;

                reader = new StreamReader(stream, Encoding.ASCII);
            }
        }

        public LogReader(Stream stream)
        {
            reader = new StreamReader(stream, Encoding.ASCII);
        }

        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            reader.Close();
            stream.Close();
        }

        public IEnumerable<string> Lines()
        {
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                    yield break;
                yield return line;
            }
        }

    }
}
