using System;
using System.Text.RegularExpressions;

/*

2019-11-20
- Added a chat filter for others' disciplines that can turn off these messages, and modified 
the existing discipline filter so you can turn off your own activate messages.

2019-04-10
- Messages indicating the start of a spell cast now include a link to the spell description.
- Spell interrupts, fizzles, and reflect messages now contain the name of the spell that failed.
- Messages indicating that a spell has worn off now include a link to the spell description.


*/

namespace EQLogParser
{
    public enum CastingType
    {
        Spell, // this includes clicks and procs
        Song,
        Disc
    }

    /// <summary>
    /// Generated when a player or NPC starts to cast a spell.
    /// </summary>
    public class LogCastingEvent : LogEvent
    {
        public string Source;
        public string Spell;
        public CastingType Type;

        public override string ToString()
        {
            return String.Format("Spell: {0} casting {1}", Source, Spell);
        }

        // [Sun May 01 08:44:59 2016] You begin casting Group Perfected Invisibility.
        private static readonly Regex CastRegex = new Regex(@"^(.+?) begins? (casting|singing) (.+)\.$", RegexOptions.Compiled);

        // [Sun Jul 05 21:33:07 2020] Rumstil activates Enraging Axe Kicks.
        private static readonly Regex DiscRegex = new Regex(@"^(.+?) activates? (.+)\.$", RegexOptions.Compiled);

        // obsolete with 2019-04-10 test server patch
        // [Sun May 01 08:44:56 2016] a woundhealer goblin begins to cast a spell. <Inner Fire>
        //private static readonly DateTime ObsoleteOtherCastMaxDate = new DateTime(2019, 4, 17);
        private static readonly Regex ObsoleteOtherCastRegex = new Regex(@"^(.+?) begins to (cast a spell|sing a song)\. <(.+)>$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        public static LogCastingEvent Parse(LogRawEvent e)
        {
            // this short-circuit exit is here strictly as a speed optmization 
            if (e.Text.IndexOf("begin", StringComparison.Ordinal) < 0 && e.Text.IndexOf("activate", StringComparison.Ordinal) < 0)
                return null;

            var m = CastRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogCastingEvent
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Spell = m.Groups[3].Value,
                    Type = m.Groups[2].Value == "singing" ?  CastingType.Song : CastingType.Spell
                };
            }

            m = DiscRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogCastingEvent
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Spell = m.Groups[2].Value,
                    Type = CastingType.Disc
                };
            }

            m = ObsoleteOtherCastRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogCastingEvent
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Spell = m.Groups[3].Value,
                    Type = m.Groups[2].Value == "sing a song" ? CastingType.Song : CastingType.Spell
                };
            }

            return null;
        }

    }

}
