using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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

        // [Sun May 01 08:44:56 2016] a woundhealer goblin begins to cast a spell. <Inner Fire>
        private static readonly Regex OtherCastRegex = new Regex(@"^(.+?) begins to (?:cast a spell|sing a song)\. <(.+)>$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Sun May 01 08:44:59 2016] You begin casting Group Perfected Invisibility.
        private static readonly Regex SelfCastRegex = new Regex(@"^You begin (?:casting|singing) (.+?)\.$", RegexOptions.Compiled);

        public static LogCastingEvent Parse(LogRawEvent e)
        {
            var m = SelfCastRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogCastingEvent
                {
                    Timestamp = e.Timestamp,
                    Source = e.Player,
                    Spell = m.Groups[1].Value
                };
            }

            m = OtherCastRegex.Match(e.Text);
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

    /// <summary>
    /// Extends LogCastingEvent with additional spell information.
    /// </summary>
    //public class LogCastingEventWithSpellInfo : LogCastingEvent
    //{
    //    public string LandSelf;
    //    public string LandOthers;
    //    public string ClassName;        
    //}

}
