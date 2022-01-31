using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when a player is rescued by a divine intervention spell.
    /// </summary>
    public class LogRescuedEvent : LogEvent
    {
        public string Target;

        public override string ToString()
        {
            return String.Format("Rescued: {0}", Target);
        }

        // [Tue Jul 07 20:21:39 2020] Rumstil has been rescued by divine intervention!
        private static readonly Regex RescueRegex = new Regex(@"^(.+?) has been rescued by divine intervention!$", RegexOptions.Compiled | RegexOptions.RightToLeft);


        public static LogRescuedEvent Parse(LogRawEvent e)
        {
            var m = RescueRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogRescuedEvent
                {
                    Timestamp = e.Timestamp,
                    Target = e.FixName(m.Groups[1].Value),
                };
            }

            return null;
        }

    }
}
