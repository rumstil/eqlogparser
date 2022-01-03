using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when you buy an AA skill.
    /// </summary>
    public class LogAAPurchaseEvent : LogEvent
    {
        public string Name;
        public int Cost;

        public override string ToString()
        {
            return String.Format("AA: {0}", Name);
        }

        // [Tue Oct 27 22:25:46 2015] You have gained the ability "Combat Fury" at a cost of 2 ability points.
        private static readonly Regex Rank1Regex = new Regex(@"^You have gained the ability ""(.+?)"" at a cost of (\d+) ability points\.$", RegexOptions.Compiled);

        // [Tue Oct 27 22:25:46 2015] You have improved Friendly Stasis 27 at a cost of 0 ability points.
        private static readonly Regex Rank2Regex = new Regex(@"^You have improved (.+?) at a cost of (\d+) ability points\.$", RegexOptions.Compiled);

        public static LogAAPurchaseEvent Parse(LogRawEvent e)
        {
            var m = Rank1Regex.Match(e.Text);
            if (m.Success)
            {
                return new LogAAPurchaseEvent
                {
                    Timestamp = e.Timestamp,
                    Name = m.Groups[1].Value,
                    Cost = Int32.Parse(m.Groups[2].Value)
                };
            }

            m = Rank2Regex.Match(e.Text);
            if (m.Success)
            {
                return new LogAAPurchaseEvent
                {
                    Timestamp = e.Timestamp,
                    Name = m.Groups[1].Value,
                    Cost = Int32.Parse(m.Groups[2].Value)
                };
            }

            return null;
        }

    }
}
