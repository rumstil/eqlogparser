using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/*

2018-12-11
- Added Rampage, Wild Rampage, and Flurry tags messages to the full round of melee attacks. 
Removed the single line messages that indicated something flurried or rampaged.
- Consolidated many chat messages that were duplicates of melee and non melee hits, including: 
Flurry, Finishing/Crippling Blow, Assassinate, Deadly Strike, Slay Undead, Headshot, and critical 
spells. These messages no longer have a separate line of text, instead they end with an indication 
of the special attack type in parenthesis.
- Added an indicator for twincasted and critical damage over time spells.

2019-02-12
- Added a (Riposte) tag to hits and misses that occurred due to a riposte.
- Throwing damage is now reported as a hit instead of a shot.
- Added the spell name and resist type to spell damage messages.
- Strikethrough messages are now reported at the end of a hit message instead of in a separate line. 
- Made the following changes to DoT reporting:
+ If the caster of a DoT dies, their damage will now be reported as being cast by their corpse.
+ If the caster zones or is charmed, their damage will be reported without a caster's name, and 
the damage will not be focused.
+ If the caster returns to the zone or is resurrected, these DoT messages will once again associate 
with them and be focused.

2019-03-05
- Direct damage spells caused by a twincast now have a flag at the end of the damage message instead 
of a separate twincast message. Twincasts of melee abilities are now indicated as twinstrikes.

*/

namespace EQLogParser
{
    /// <summary>
    /// Generated when a damage attempt is successful (can be melee or spell damage).
    /// </summary>
    public class LogHitEvent : LogEvent
    {
        public string Source;
        public string Target;
        public int Amount;
        public string Type;
        public string Special; // crit, finishing blow, etc..
        public string Spell;

        public override string ToString()
        {
            return String.Format("Hit: {0} => {1} ({2}) {3} {4}", Source, Target, Amount, Type, Spell);
        }

        // [Fri Dec 28 16:30:41 2018] A tree snake bites Lenantik for 470 points of damage.
        // [Fri Dec 28 23:31:14 2018] You shoot a Sebilisian bonecaster for 10717 points of damage. (Critical Double Bow Shot)
        // this has a negative look ahead to avoid matching obsolete nuke damage messages
        private static readonly Regex MeleeHitRegex = new Regex(@"^(.+?) (hit|shoot|kick|slash|crush|pierce|bash|slam|strike|punch|backstab|bite|claw|smash|slice|gore|maul|rend|burn|sting|frenzy on|frenzies on)e?s? (?!by non-melee)(.+?) for (\d+) points? of damage\.(?:\s\((.+?)\))?$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Fri Dec 28 16:30:43 2018] A shadow wolf is pierced by YOUR thorns for 113 points of non-melee damage.
        // [Fri Dec 28 15:43:53 2018] A lizard is burned by YOUR flames for 380 points of non-melee damage.
        private static readonly Regex DamageShieldRegex = new Regex(@"^(.+?) is \w+ by YOUR \w+ for (\d+) points? of non-melee damage\.$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // obsolete with 2019-02-12 test server patch
        // [Fri Dec 28 23:12:03 2018] Rumstil hit a scaled wolf for 726 points of non-melee damage.
        private static readonly Regex ObsoleteNukeDamageRegex = new Regex(@"^(.+?) hit (.+?) for (\d+) points? of non-melee damage\.(?:\s\((.+?)\))?$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Wed Feb 13 21:31:06 2019] Tokiel hit a tirun crusher for 21617 points of chromatic damage by Mana Repetition Strike.
        private static readonly Regex NukeDamageRegex = new Regex(@"^(.+?) hit (.+?) for (\d+) points? of \w+ damage by (.+?)\.(?:\s\((.+?)\))?", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Sun May 08 20:13:09 2016] An aggressive corpse has taken 212933 damage from your Mind Storm.
        // [Wed Feb 20 18:35:31 2019] A Drogan berserker has taken 34993 damage from Mind Tempest by Fourier.
        // ignore dots that don't have a source (caster has died/zoned)
        private static readonly Regex OwnDoTDamageRegex = new Regex(@"^(.+?) has taken (\d+) damage from your (.+?)\.(?:\s\((.+?)\))?$", RegexOptions.Compiled);
        private static readonly Regex OtherDoTDamageRegex = new Regex(@"^(.+?) has taken (\d+) damage from (.+?) by (.+?)\.(?:\s\((.+?)\))?$", RegexOptions.Compiled);

        public static LogHitEvent Parse(LogRawEvent e)
        {
            var m = MeleeHitRegex.Match(e.Text);
            if (m.Success)
            {
                var type = m.Groups[2].Value;
                if (type == "frenzy on" || type == "frenzies on")
                    type = "frenzy";
                var special = m.Groups[5].Success ? m.Groups[5].Value.ToLower() : null;
                // some special killshot type attacks skew damage so much that it's better to classify them as their own type
                if (special?.Contains("finishing blow") == true)
                {
                    type = "finishing";
                    special = null;
                }

                return new LogHitEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Type = type,
                    Target = e.FixName(m.Groups[3].Value),
                    Amount = Int32.Parse(m.Groups[4].Value),
                    Special = special
                };
            }

            m = NukeDamageRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogHitEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Target = e.FixName(m.Groups[2].Value),
                    Amount = Int32.Parse(m.Groups[3].Value),
                    Spell = m.Groups[4].Value,
                    Type = "dd",
                    Special = m.Groups[5].Success ? m.Groups[5].Value.ToLower() : null
                };
            }
            
            m = OwnDoTDamageRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogHitEvent()
                {
                    Timestamp = e.Timestamp,
                    Target = e.FixName(m.Groups[1].Value),
                    Amount = Int32.Parse(m.Groups[2].Value),
                    Source = e.Player,
                    Spell = m.Groups[3].Value,
                    Type = "dot",
                    Special = m.Groups[4].Success ? m.Groups[4].Value.ToLower() : null
                };
            }

            m = OtherDoTDamageRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogHitEvent()
                {
                    Timestamp = e.Timestamp,
                    Target = e.FixName(m.Groups[1].Value),
                    Amount = Int32.Parse(m.Groups[2].Value),
                    Source = m.Groups[4].Value,
                    Spell = m.Groups[3].Value,
                    Type = "dot",
                    Special = m.Groups[5].Success ? m.Groups[5].Value.ToLower() : null
                };
            }

            m = DamageShieldRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogHitEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.Player,
                    Target = m.Groups[1].Value,
                    Amount = Int32.Parse(m.Groups[2].Value),
                    Type = "dmgshield"
                };
            }

            // this is obsolete but doesn't interfere with any current log messages and has minimal impact on parsing performance
            m = ObsoleteNukeDamageRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogHitEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Target = e.FixName(m.Groups[2].Value),
                    Amount = Int32.Parse(m.Groups[3].Value),
                    Type = "dd",
                    Special = m.Groups[4].Success ? m.Groups[4].Value.ToLower() : null
                };
            }

            return null;
        }

    }
}
