using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when you earn an AAXP point.
    /// </summary>
    public class LogAAXPEvent : LogEvent
    {
        public int Amount;
        public int Total;

        public override string ToString()
        {
            return String.Format("AAXP: {0}", Amount);
        }

        // [Tue Jan 01 17:35:51 2019] You have gained 2 ability point(s)!  You now have 39 ability point(s).
        private static readonly Regex AAXPRegex = new Regex(@"^You have gained (\d+) ability point\(s\)!  You now have (\d+) ability point\(s\).$", RegexOptions.Compiled);

        public static LogAAXPEvent Parse(LogRawEvent e)
        {
            var m = AAXPRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogAAXPEvent
                {
                    Timestamp = e.Timestamp,
                    Amount = Int32.Parse(m.Groups[1].Value),
                    Total = Int32.Parse(m.Groups[2].Value)
                };
            }

            return null;
        }

    }
}
