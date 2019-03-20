using System;
using System.Text.RegularExpressions;

/*
2019-1-16
- Spell and combat ability resist messages will now include the name of the target (if you are 
the caster) or the name of the caster (if you are the target.) Additionally, the name of the 
spell will now function as a link to the spell description.
Ex: 'You resist the Skunk Spray spell!' is now 'You resist a large skunk's Skunk Spray!'
Ex: 'Your target resisted the Fireball spell.' is now 'A large skunk resisted your Fireball!'

*/

namespace EQLogParser
{
    /// <summary>
    /// Generated when a damage attempt fails and does no damage (can be a miss, defense, or spell resist).
    /// </summary>
    public class LogMissEvent : LogEvent
    {
        public string Source;
        public string Target;
        public string Type;
        public string Special;
        public string Spell;

        public override string ToString()
        {
            return String.Format("Miss: {0} => {1} {2}", Source, Target, Type);
        }

        // [Thu Apr 21 20:56:17 2016] Commander Alast Degmar tries to punch YOU, but misses!
        // [Thu May 19 10:30:15 2016] A darkmud watcher tries to hit Rumstil, but misses!
        // [Thu May 19 15:32:30 2016] You try to pierce an ocean serpent, but miss!
        // [Thu May 19 15:32:23 2016] An ocean serpent tries to hit YOU, but YOU parry!
        // [Thu May 19 15:31:33 2016] A sea naga stormcaller tries to hit Fourier, but Fourier's magical skin absorbs the blow!
        // [Fri Dec 28 23:31:08 2018] You try to shoot a sarnak conscript, but miss! (Double Bow Shot)
        //private static readonly Regex MeleeMissRegex = new Regex(@"^(.+?)(?: try to | tries to )(\w+)(?: on)? (.+?), but .*?(miss|riposte|parry|parries|dodge|block|magical skin absorbs the blow)e?s?!$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        private static readonly Regex MeleeMissRegex = new Regex(@"^(.+?) (?:try|tries) to (\w+)(?: on)? (.+?), but .*?(miss|riposte|parry|parries|dodge|block|INVULNERABLE|magical skin absorbs the blow)e?s?!(?:\s\((.+?)\))?$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        private static readonly Regex ResistRegex = new Regex(@"^(.+?) resisted your (.+?)!$", RegexOptions.Compiled);
        private static readonly Regex SelfResistRegex = new Regex(@"^You resist (.+?)'s (.+?)!$", RegexOptions.Compiled);
        public static LogMissEvent Parse(LogRawEvent e)
        {
            var m = MeleeMissRegex.Match(e.Text);
            if (m.Success)
            {
                var type = m.Groups[4].Value;
                if (type == "parries")
                    type = "parry";
                if (type == "magical skin absorbs the blow")
                    type = "rune";
                if (type == "INVULNERABLE")
                    type = "invul";

                return new LogMissEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Target = e.FixName(m.Groups[3].Value),
                    Type = type,
                    Special = m.Groups[5].Success ? m.Groups[5].Value.ToLower() : null
                };
            }

            m = ResistRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogMissEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.Player,
                    Target = e.FixName(m.Groups[1].Value),
                    Type = "resist",
                    Spell = m.Groups[2].Value
                };
            }

            m = SelfResistRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogMissEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Target = e.Player,
                    Type = "resist",
                    Spell = m.Groups[2].Value
                };
            }

            return null;
        }

    }
}
