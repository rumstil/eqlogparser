using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when a player gets a skill up.
    /// </summary>
    public class LogSkillEvent : LogEvent
    {
        public string Name;
        public int Level;

        public override string ToString()
        {
            return String.Format("Zone: {0}", Name);
        }

        // [Wed Feb 13 21:31:20 2019] You have become better at Specialize Divination! (53)
        private static readonly Regex SkillRegex = new Regex(@"^You have become better at (.+)! \((\d+)\)$", RegexOptions.Compiled);

        // [Mon Mar 18 23:29:08 2019] Your guildmate Smiddy has completed Smithing (50) achievement.

        public static LogSkillEvent Parse(LogRawEvent e)
        {
            var m = SkillRegex.Match(e.Text);
            if (m.Success)
            {
                var skill = m.Groups[1].Value;
                return new LogSkillEvent
                {
                    Timestamp = e.Timestamp,
                    Name = m.Groups[1].Value,
                    Level = Int32.Parse(m.Groups[2].Value)
                };
            }

            return null;
        }

    }
}
