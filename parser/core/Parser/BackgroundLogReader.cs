using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace EQLogParser
{
    public class LogReaderStatus
    {
        //public string Path;
        public double Percent;
        public int Line;
        public string Notes;
    }


    /// <summary>
    /// A log reader that works in a background task.
    /// Lines are passed to an delegate for further processing.
    /// </summary>
    public class BackgroundLogReader
    {
        private readonly CancellationToken cancellationToken;
        private readonly IProgress<LogReaderStatus> progress;
        private readonly StreamReader reader;
        private readonly FileStream stream;
        private readonly Action<string> enqueue;

        /// <summary>
        /// Init a new instance of the background log reader.
        /// </summary>
        /// <param name="enqueue">A delegate that runs in the background context and must be threadsafe.</param>
        public BackgroundLogReader(string path, CancellationToken ct, IProgress<LogReaderStatus> progress, Action<string> enqueue)
        {
            this.cancellationToken = ct;
            this.progress = progress;
            this.enqueue = enqueue;
            stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (path.EndsWith(".gz"))
            {
                var gzip = new GZipStream(stream, CompressionMode.Decompress);
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
        /// Progress is reported every 500 events or 500ms (whichever comes first)
        /// </summary>
        private void Run()
        {
            try
            {
                var timer = Stopwatch.StartNew();
                int count = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    // not going to use async here since I only have 1 instance of the background task 
                    // and it's okay if it blocked
                    var line = reader.ReadLine();

                    // if the end of the stream is reached then report progress and go to sleep for a while
                    if (line == null)
                    {
                        timer.Stop();
                        progress.Report(new LogReaderStatus() { Percent = (float)stream.Position / stream.Length, Line = count, Notes = timer.Elapsed.TotalSeconds.ToString("F1") + "s" }); ;
                        Thread.Sleep(500);
                        continue;
                    }

                    // send to queue
                    enqueue(line);

                    // report progress (during initial bulk read where there are no sleeps)
                    count += 1;
                    if (count % 500 == 0)
                    {
                        progress.Report(new LogReaderStatus() { Percent = (float)stream.Position / stream.Length, Line = count, Notes = timer.Elapsed.TotalSeconds.ToString("F1") + "s" }); ;
                    }
                }

            }
            finally
            {
                reader.Close();
                stream.Close();
            }
        }

    }
}
