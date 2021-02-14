using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when a warrior uses their /shield ability.
    /// </summary>
    public class LogShieldEvent : LogEvent
    {
        public string Source;
        public string Target;

        public override string ToString()
        {
            return String.Format("Shield: {0} => {1}", Source, Target);
        }

        // [Tue Jul 07 20:21:39 2020] Technician Masterwork begins to use a steamwork trooper as a living shield!
        private static readonly Regex ShieldStartRegex = new Regex(@"^(.+?) begins to use (.+?) as a living shield!$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Sat Sep 19 18:40:57 2020] An ancient iksar ceases protecting an ancient iksar.
        private static readonly Regex ShieldStopRegex = new Regex(@"^(.+?) ceases protecting (.+?).$", RegexOptions.Compiled);


        public static LogShieldEvent Parse(LogRawEvent e)
        {
            var m = ShieldStartRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogShieldEvent
                {
                    Timestamp = e.Timestamp,
                    Target = e.FixName(m.Groups[1].Value.Replace("'s corpse", "")),
                    Source = e.FixName(m.Groups[2].Value.Replace("'s corpse", "")),
                };
            }

            return null;
        }

    }
}
