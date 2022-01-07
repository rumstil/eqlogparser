using System;
using System.Text.RegularExpressions;

/*
2019-1-16
- Spell and combat ability resist messages will now include the name of the target (if you are 
the caster) or the name of the caster (if you are the target.) Additionally, the name of the 
spell will now function as a link to the spell description.
Ex: 'You resist the Skunk Spray spell!' is now 'You resist a large skunk's Skunk Spray!'
Ex: 'Your target resisted the Fireball spell.' is now 'A large skunk resisted your Fireball!'

2019-2-20
- Added a (Riposte) tag to hits and misses that occurred due to a riposte.

2019-4-17
- Riposte messages are now reported before their resulting hit damage.

2019-9-18
- The hit/miss check in melee combat now occurs before riposte and other defensive checks. (test wording)
- You can no longer block, parry, dodge, or riposte an attack that would have missed. (live wording)



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
        public LogEventMod Mod;
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
        // [Sat Feb 23 14:56:59 2019] A grove guardian tries to smash Jantik, but Jantik's magical skin absorbs the blow! (Riposte Strikethrough)
        // [Fri Dec 28 23:31:08 2018] You try to shoot a sarnak conscript, but miss! (Double Bow Shot)
        private static readonly Regex MeleeMissRegex = new Regex(@"^(.+) \w+ to (\w+)(?: on)? (.+?), but .*?(miss|riposte|parry|parries|dodge|block|blocks with \w\w\w shield|INVULNERABLE|magical skin absorbs the blow)e?s?!(?:\s\(([^\(\)]+)\))?$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Mon Mar 25 21:55:36 2019] YOUR magical skin absorbs the damage of a Bloodmoon boneeater's thorns.


        private static readonly Regex ResistRegex = new Regex(@"^(.+) resisted your (.+?)!$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        private static readonly Regex SelfResistRegex = new Regex(@"^You resist (.+?)'s (.+)!$", RegexOptions.Compiled);

        public static LogMissEvent Parse(LogRawEvent e)
        {
            // this short-circuit exit is here strictly as an optmization 
            if (!e.Text.Contains(", but", StringComparison.Ordinal) && !e.Text.Contains("resist", StringComparison.Ordinal))
                return null;

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
                if (type.StartsWith("blocks with"))
                    type = "shield";

                return new LogMissEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Target = e.FixName(m.Groups[3].Value),
                    Type = type,
                    Mod = ParseMod(m.Groups[5].Value)
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

            return null;
        }

        private static LogEventMod ParseMod(string text)
        {
            LogEventMod mod = 0;
            var parts = text.ToLower().Split(' ');
            for (int i = 0; i < parts.Length; i++)
            {
                switch (parts[i])
                {
                    case "lucky":
                        mod |= LogEventMod.Lucky;
                        break;
                    case "riposte":
                        mod |= LogEventMod.Riposte;
                        break;
                    case "strikethrough":
                        mod |= LogEventMod.Strikethrough;
                        break;
                    default:
                        break;
                }
            }
            return mod;
        }




    }
}
