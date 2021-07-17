using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated after a successful taunt.
    /// </summary>
    public class LogTauntEvent : LogEvent
    {
        /// <summary>
        /// The taunter.
        /// </summary>
        public string Source;

        /// <summary>
        /// The taunted mob. Target will be null for an AE taunt.
        /// </summary>
        public string Target;

        public override string ToString()
        {
            return String.Format("Taunt: {0} => {1}", Source, Target);
        }

        // [Thu Jul 08 22:55:32 2021] Rumstil has captured Master Yael's attention!
        // [Sun Jun 27 16:17:48 2021] Rumstil was partially successful in capturing Cazic-Thule's attention.
        private static readonly Regex TauntRegex = new Regex(@"^(\w+) (?:capture|has captured|was partially successful in capturing) (.+)'s attention[!\.]$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Thu Jul 08 20:38:25 2021] Rumstil has captured Gorgalosk's attention with an unparalleled approach!
        // [Sun Jul 11 21:57:05 2021] You capture a mortiferous golem's attention with your unparalleled reproach!
        private static readonly Regex CriticalTauntRegex = new Regex(@"^(\w+) (?:capture|has captured) (.+)'s attention with (an unparalleled approach|your unparalleled reproach)!$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Sun Jun 27 15:55:20 2021] Rumstil captures the attention of everything in the area!
        private static readonly Regex AETauntRegex = new Regex(@"^(\w+) captures the attention of everything in the area!$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        public static LogTauntEvent Parse(LogRawEvent e)
        {
            var m = TauntRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogTauntEvent
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Target = e.FixName(m.Groups[2].Value),
                };
            }

            m = CriticalTauntRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogTauntEvent
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Target = e.FixName(m.Groups[2].Value),
                };
            }

            m = AETauntRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogTauntEvent
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Target = null,
                };
            }

            return null;
        }

    }
}
