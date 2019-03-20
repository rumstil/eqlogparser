using System;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /*
    /// <summary>
    /// This is an experiment to call parsers via attrib registration rather than explicitly calling each parser.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class LogRawEventParserAttribute : Attribute
    {
        public DateTime? MinDate { get; set; }
        public DateTime? MaxDate { get; set; }
    }
    */

    /// <summary>
    /// Root class for all log events.
    /// </summary>
    public abstract class LogEvent
    {
        /// <summary>
        /// A unique id assigned to each log event.
        /// </summary>
        public int Id;

        /// <summary>
        /// Timestamp as it appears in the log file. Converted to UTC.
        /// </summary>
        public DateTime Timestamp;
    }

    /// <summary>
    /// Represents a raw log line with the timestamp and text split up. 
    /// This is used as the input for all other parsers.
    /// </summary>
    public class LogRawEvent : LogEvent
    {
        /// <summary>
        /// Player name as it appear in log filename.
        /// </summary>
        public string Player;

        /// <summary>
        /// Raw text as it appears in the log file.
        /// </summary>
        public string Text;

        // [Tue Nov 03 21:41:50 2015] Welcome to EverQuest!
        //private static readonly Regex LinePartsRegex = new Regex(@"^\[\w{3} (.{20})\] (.+)$", RegexOptions.Compiled);

        /// <summary>
        /// Parse a full line from the log file into a timestamp and text.
        /// </summary>
        public static LogRawEvent Parse(string text)
        {
            // it's a tiny bit faster to use substrings over a regex match here
            //var m = LinePartsRegex.Match(text);
            //if (!m.Success)
            //    return null;

            //if (!DateTime.TryParseExact(m.Groups[1].Value, "MMM dd HH:mm:ss yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeLocal, out DateTime ts))
            if (text.Length < 25 || !DateTime.TryParseExact(text.Substring(5, 20), "MMM dd HH:mm:ss yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeLocal, out DateTime ts))
                return null;

            return new LogRawEvent()
            {
                Timestamp = ts.ToUniversalTime(),
                //Text = m.Groups[2].Value
                Text = text.Substring(27)
            };
        }

        public LogRawEvent()
        {

        }

        public LogRawEvent(string text)
        {
            Text = text;
        }

        public override string ToString()
        {
            return String.Format("???: {0}", Text);
        }

        /// <summary>
        /// Normalize character name.
        /// </summary>
        public string FixName(string name)
        {
            if (String.IsNullOrEmpty(name))
                return null;

            if (name.Equals("you", StringComparison.InvariantCultureIgnoreCase) ||
                name.Equals("your", StringComparison.InvariantCultureIgnoreCase))
                return Player;

            // replace backticks just because they look weird
            // perhaps this is a bad idea since downstream code may look for "x`s warder" etc...
            //name = name.Replace('`', '\'');

            // a few log messages can reference a corpse if they occur after the pc/npc died
            //if (name.EndsWith(CorpseSuffix))
            //    name = name.Substring(0, name.Length - CorpseSuffix.Length);

            // many log messages will uppercase the first letter in a mob's name
            // so we will normalize names to always start with an uppercased char
            if (Char.IsLower(name[0]))
                name = Char.ToUpper(name[0]) + name.Substring(1);

            return name;
        }
    }

}
