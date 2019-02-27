using System;
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
        StreamReader Stream;
        FileSystemWatcher Watch;
        int LastReadTicks;

        public LogReader(string path)
        {
            Path = path;
            //Stream = File.OpenText(path);
            Stream = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.ASCII);
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
            StopWatcherThread();
            Stream.Close();
        }

        /// <summary>
        /// Read all lines in the file from the last place we left off and pass them to the OnRead callback.
        /// </summary>
        public void ReadAllLines()
        {
            // make sure the watcher thread isn't also running
            //if (Watch != null && Watch.EnableRaisingEvents)
            //    throw new InvalidOperationException("Do not call ReadAllLines while watcher is active.");

            //while (!Stream.EndOfStream)
            while (true)
            {
                var line = Stream.ReadLine();
                if (line == null)
                    return;
                OnRead(line);
                LastReadTicks = Environment.TickCount;
            }
        }

        /// <summary>
        /// Start background file watcher thread and continue to read lines whenever the file is updated.
        /// </summary>
        public void StartWatcherThread()
        {
            if (Watch == null)
                Watch = new FileSystemWatcher(System.IO.Path.GetDirectoryName(Path), System.IO.Path.GetFileName(Path));
            Watch.IncludeSubdirectories = false;
            Watch.Changed += ChangedHandler;
            Watch.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Stop the background file watcher thread.
        /// </summary>
        public void StopWatcherThread()
        {
            if (Watch != null)
                Watch.EnableRaisingEvents = false;
        }

        private void ChangedHandler(object source, FileSystemEventArgs e)
        {
            // throttle reader (notification event can be generated several time per write)
            var elapsed = Environment.TickCount - LastReadTicks;
            if (elapsed > 0 && elapsed < 500)
                return;

            //Console.WriteLine("--- {0} {1}", e.ChangeType, e.FullPath);
            ReadAllLines();
        }


    }
}
