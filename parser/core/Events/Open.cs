using System;
using System.Collections.Generic;
using System.Text;

namespace EQLogParser
{
    /// <summary>
    /// This should be sent to the trackers as the first HandleEvent() call after a log file is opened to ensure proper initialization.
    /// Unlike other events which are all returned from the parser, it should fall to the log reader to generate this event.
    /// </summary>
    public class LogOpenEvent : LogEvent
    {
        public string Path;

        /// <summary>
        /// Player name as it appear in log filename.
        /// </summary>
        public string Player;

        /// <summary>
        /// Server name as it appear in log filename.
        /// </summary>
        public string Server;

    }
}
