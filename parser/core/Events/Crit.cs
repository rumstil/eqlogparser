using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    public enum LogCritSequence
    {
        BeforeHit,
        AfterHit
    }

    /// <summary>
    /// Generated when a damage hit has a critical success.
    /// These log entries are no longer used as of the Dec 2018 release of TBL.
    /// Melee crits are logged before the hit.
    /// Nuke crits are logged after the hit.
    /// </summary>
    [Obsolete]
    public class LogCritEvent : LogEvent
    {
        private static readonly DateTime MaxDate = DateTime.Parse("2018-12-18");

        public string Source;
        public int Amount;
        public LogCritSequence Sequence;

        public override string ToString()
        {
            return String.Format("HitCrit: {0} => {1}", Source, Amount);
        }

        // melee critical hits are always in 3rd person 
        // [Tue Nov 03 22:09:18 2015] Rumstil scores a critical hit! (8786) -- shown before actual hit, always 3rd person
        private static readonly Regex MeleeCriticalRegex = new Regex(@"^(.+?)(?: scores a critical hit! | lands a Crippling Blow!| scores a Deadly Strike!|'s holy blade cleanses h\w\w target!)\((\d+)\)$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // spell criticals are in both first and 3rd person (if others criticals option is enabled)
        // [Tue Nov 03 22:11:19 2015] You deliver a critical blast! (16956) -- shown after actual spell
        private static readonly Regex SpellCriticalRegex = new Regex(@"^(.+?) delivers? a critical blast! \((\d+)\)$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        public static LogCritEvent Parse(LogRawEvent e)
        {
            if (e.Timestamp > MaxDate)
                return null;

            var m = MeleeCriticalRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogCritEvent
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Amount = Int32.Parse(m.Groups[2].Value),
                    Sequence = LogCritSequence.BeforeHit
                };
            }

            m = SpellCriticalRegex.Match(e.Text);
            if (m.Success)
            {
                // if others crits are on, the game will produce 2 versions of the critical message
                // we can ignore one of them (the 3rd party one)
                if (m.Groups[1].Value != e.Player)
                    return new LogCritEvent
                    {
                        Timestamp = e.Timestamp,
                        Source = e.FixName(m.Groups[1].Value),
                        Amount = Int32.Parse(m.Groups[2].Value),
                        Sequence = LogCritSequence.AfterHit
                    };
            }

            return null;
        }

    }
}
