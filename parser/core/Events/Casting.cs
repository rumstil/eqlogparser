using System;
using System.Text.RegularExpressions;

/*

2019-04-10
- Messages indicating the start of a spell cast now include a link to the spell description.
- Spell interrupts, fizzles, and reflect messages now contain the name of the spell that failed.
- Messages indicating that a spell has worn off now include a link to the spell description.

*/

namespace EQLogParser
{
    /// <summary>
    /// Generated when a player or NPC starts to cast a spell.
    /// </summary>
    public class LogCastingEvent : LogEvent
    {
        public string Source;
        public string Spell;
        //public string Cancelled;

        public override string ToString()
        {
            return String.Format("Spell: {0} casting {1}", Source, Spell);
        }

        // [Sun May 01 08:44:59 2016] You begin casting Group Perfected Invisibility.
        private static readonly Regex CastRegex = new Regex(@"^(.+?) begins? (?:casting|singing) (.+)\.$", RegexOptions.Compiled);

        // obsolete with 2019-04-10 test server patch
        // [Sun May 01 08:44:56 2016] a woundhealer goblin begins to cast a spell. <Inner Fire>
        //private static readonly DateTime ObsoleteOtherCastMaxDate = new DateTime(2019, 4, 17);
        private static readonly Regex ObsoleteOtherCastRegex = new Regex(@"^(.+?) begins to (?:cast a spell|sing a song)\. <(.+)>$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        public static LogCastingEvent Parse(LogRawEvent e)
        {
            var m = CastRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogCastingEvent
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Spell = m.Groups[2].Value
                };
            }

            m = ObsoleteOtherCastRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogCastingEvent
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Spell = m.Groups[2].Value
                };
            }

            return null;
        }

    }

}
