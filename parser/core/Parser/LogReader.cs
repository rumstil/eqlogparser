using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EQLogParser
{
    public class LogReader : IDisposable    
    {
        public readonly string Path;
        private readonly StreamReader reader;
        private readonly GZipStream gzip;
        private readonly FileStream stream;

        public float PercentRead => (float)stream.Position / stream.Length;

        public LogReader(string path, int index = 0)
        {
            Path = path;

            stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            if (path.EndsWith(".gz"))
            {
                gzip = new GZipStream(stream, CompressionMode.Decompress);
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
            reader?.Close();
            gzip?.Close();
            stream?.Close();
        }

        public string ReadLine()
        {
            return reader.ReadLine();
        }

        public Task<string> ReadLineAsync()
        {
            return reader.ReadLineAsync();
        }

    }
}
