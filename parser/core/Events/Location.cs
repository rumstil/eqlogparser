using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when a /loc update is received.
    /// EQ displays coordinates in Y, X, Z order.
    /// </summary>
    public class LogLocationEvent : LogEvent
    {
        public int X;
        public int Y;
        public int Z;

        public override string ToString()
        {
            return String.Format("Loc: {0}, {1}, {2}", Y, X, Z);
        }

        // [Mon Mar 21 21:44:57 2016] Your Location is 1131.16, 1089.94, 162.74
        private static readonly Regex LocationRegex = new Regex(@"^Your Location is (-?\d+).+?, (-?\d+).+?, (-?\d+)", RegexOptions.Compiled);

        public static LogLocationEvent Parse(LogRawEvent e)
        {
            var m = LocationRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogLocationEvent
                {
                    Timestamp = e.Timestamp,
                    Y = Int32.Parse(m.Groups[1].Value),
                    X = Int32.Parse(m.Groups[2].Value),
                    Z = Int32.Parse(m.Groups[3].Value)
                };
            }

            return null;
        }

    }
}
