using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when a player zones.
    /// </summary>
    public class LogZoneEvent : LogEvent
    {
        public string Name;

        public override string ToString()
        {
            return String.Format("Zone: {0}", Name);
        }

        // [Tue Nov 03 21:41:54 2015] You have entered Plane of Knowledge.
        private static readonly Regex ZoneChangedRegex = new Regex(@"^You have entered (.+)\.$", RegexOptions.Compiled);

        public static LogZoneEvent Parse(LogRawEvent e)
        {
            var m = ZoneChangedRegex.Match(e.Text);
            if (m.Success)
            {
                var zone = m.Groups[1].Value;
                if (zone == "an area where levitation effects do not function" ||
                    zone == "an Arena (PvP) area" ||
                    zone == "the Drunken Monkey stance adequately")
                    return null;

                return new LogZoneEvent
                {
                    Timestamp = e.Timestamp,
                    Name = zone
                };
            }

            return null;
        }

    }
}
