using System;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Base class for all log events.
    /// </summary>
    public abstract class LogEvent
    {
        /// <summary>
        /// Timestamp as it appears in the log file. Converted to UTC.
        /// </summary>
        public DateTime Timestamp;

    }

}
