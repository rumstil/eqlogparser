using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/*
2022-08-17
Added messaging to the Heals (You) and Heals (Others) channels when a debuff with counters is cured.

*/

namespace EQLogParser
{
    /// <summary>
    /// Generated when detrimental spell is cured.
    /// </summary>
    public class LogCureEvent : LogEvent
    {
        public string Source;
        public string Target;
        public string Spell;
        
        public override string ToString()
        {
            return String.Format("Cure: {0} => {1} ({2})", Source, Target, Spell);
        }

        // [Thu Aug 18 20:05:40 2022] Griklor the Restless is cured of Hemorrhagic Venom Rk. III by Griklor the Restless.
        private static readonly Regex CureRegex = new Regex(@"^(.+?) (?:is|are) cured of (.+?) by (.+?)\.$", RegexOptions.Compiled);

        public static LogCureEvent Parse(LogRawEvent e)
        {
            // this short-circuit exit is here strictly as a speed optmization 
            if (e.Text.IndexOf("cured", StringComparison.Ordinal) < 0)
                return null;

            var m = CureRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogCureEvent()
                {
                    Timestamp = e.Timestamp,
                    Target = e.FixName(m.Groups[1].Value),
                    Spell = m.Groups[2].Value,
                    Source = e.FixName(m.Groups[3].Value),
                };
            }

            return null;
        }
    }
}
