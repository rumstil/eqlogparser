using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when someone dies (can be player, pet or NPC)
    /// </summary>
    public class LogDeathEvent : LogEvent
    {
        public string Name;
        public string KillShot;

        public override string ToString()
        {
            return String.Format("Death: {0}", Name);
        }

        // [Tue Nov 03 22:34:34 2015] You have been slain by a sneaky escort!
        // [Tue Nov 03 22:34:38 2015] Rumstil has been slain by a supply guardian!
        private static readonly Regex DeathRegex = new Regex(@"^(.+?) (?:have|has) been slain by (.+?)!$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Thu May 12 17:16:25 2016] You have slain a slag golem!
        private static readonly Regex DeathRegex2 = new Regex(@"^You have slain (.+?)!$", RegexOptions.Compiled);

        // [Thu May 26 14:09:39 2016] a loyal reaver died.
        private static readonly Regex DeathRegex3 = new Regex(@"^(.+?) died\.$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        public static LogDeathEvent Parse(LogRawEvent e)
        {
            var m = DeathRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogDeathEvent
                {
                    Timestamp = e.Timestamp,
                    Name = e.FixName(m.Groups[1].Value),
                    KillShot = e.FixName(m.Groups[2].Value)
                };
            }

            m = DeathRegex2.Match(e.Text);
            if (m.Success)
            {
                return new LogDeathEvent
                {
                    Timestamp = e.Timestamp,
                    Name = e.FixName(m.Groups[1].Value),
                    KillShot = e.Player
                };
            }

            m = DeathRegex3.Match(e.Text);
            if (m.Success)
            {
                return new LogDeathEvent
                {
                    Timestamp = e.Timestamp,
                    Name = e.FixName(m.Groups[1].Value)
                };
            }

            return null;
        }

    }
}
