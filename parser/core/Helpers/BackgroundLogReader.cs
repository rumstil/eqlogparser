using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace EQLogParser.Helpers
{
    public class LogReaderStatus
    {
        //public string Path;
        public double Percent;
        public int Line;
        public string Notes;
    }


    /// <summary>
    /// A log reader that creates a background task for reading.
    /// Lines are passed to a delegate for further processing.
    /// Reader will sleep for 100ms whenever it reaches the end of the file before resuming.
    /// </summary>
    public class BackgroundLogReader
    {
        private readonly string path;
        private readonly CancellationToken cancellationToken;
        private readonly IProgress<LogReaderStatus> progress;
        private readonly StreamReader reader;
        private readonly GZipStream gzip;
        private readonly FileStream stream;
        private readonly Action<string> handler;
        private double percent;
        private double elapsed;

        public double Percent => percent;
        public double Elapsed => elapsed;

        /// <summary>
        /// Create a new instance of the background log reader.
        /// </summary>
        public BackgroundLogReader(string path, CancellationToken ct, Action<string> handler, IProgress<LogReaderStatus> progress = null)
        {
            this.cancellationToken = ct;
            this.progress = progress;
            this.handler = handler;
            this.path = path;
            stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (path.EndsWith(".gz"))
            {
                gzip = new GZipStream(stream, CompressionMode.Decompress);
                reader = new StreamReader(gzip, Encoding.ASCII);
            }
            else
            {
                reader = new StreamReader(stream, Encoding.ASCII);
            }
        }

        /// <summary>
        /// Start the log reader task. 
        /// </summary>
        public Task Start()
        {
            return Task.Factory.StartNew(Run, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Read the log file until a cancellation is requested.
        /// </summary>
        private void Run()
        {
            try
            {
                var timer = Stopwatch.StartNew();
                int count = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    // not going to use async read here since I only have 1 instance of the background task 
                    // and it's okay if it blocked
                    var line = reader.ReadLine();

                    // report progress
                    if (line == null || count % 500 == 0)
                    {
                        percent = (float)stream.Position / stream.Length;
                        elapsed = timer.Elapsed.TotalSeconds;
                        progress?.Report(new LogReaderStatus() 
                        { 
                            Percent = percent, 
                            Line = count, 
                            Notes = elapsed.ToString("F1") + "s" 
                        });
                    }

                    // if the end of the stream is reached then sleep for a while
                    if (line == null)
                    {
                        timer.Stop();
                        Thread.Sleep(100);
                        continue;
                    }

                    // process line
                    handler(line);
                    count += 1;
                }

            }
            finally
            {
                reader?.Close();
                gzip?.Close();
                stream?.Close();
            }
        }

    }
}
