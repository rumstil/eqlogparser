﻿using System;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

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
-- If the caster of a DoT dies, their damage will now be reported as being cast by their corpse.
-- If the caster zones or is charmed, their damage will be reported without a caster's name, and 
the damage will not be focused.
-- If the caster returns to the zone or is resurrected, these DoT messages will once again associate 
with them and be focused.

2019-03-05
- Direct damage spells caused by a twincast now have a flag at the end of the damage message instead 
of a separate twincast message. Twincasts of melee abilities are now indicated as twinstrikes.

2019-04-10
- Increased the range that direct-damage and damage-over-time spell messages are reported from 
75 feet to 200 feet.
- DoT spells once again display a twincast message when the spell first lands, and no longer 
reassociate with their caster if they zone or die.
- Riposte messages are now reported before their resulting hit damage.
- Dealing direct-damage with spells now reports the caster as 'You' rather than your name.

2022-01-04
https://forums.daybreakgames.com/eq/index.php?threads/testing-request-locked-hp.280167/#post-4095231
NPCs with locked HP will now report damage they are taking more accurately. Damage will include 
the (Locked) tag if it was a partial hit on an NPC with locked HP. If no damage occurred, it will not be reported.

There are a couple issues with it that will hopefully be fixed tomorrow:
- Hits should no longer display other tags if the (Locked) tag is displayed (no Locked Lucky Critical Twincast).
- Direct damage spells should no longer be reported twice if their damage was reduced due to an NPC having locked HP.
- Twincasts should no longer incorrectly report as also being (Locked)

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
        public LogEventMod Mod;
        public string Spell;

        public override string ToString()
        {
            return String.Format("Hit: {0} => {1} ({2}) {3} {4}", Source, Target, Amount, Type, Spell);
        }

        private static readonly Regex HitModRegex = new Regex(@"\(([^\(\)]+)\)?$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Fri Dec 28 16:30:41 2018] A tree snake bites Lenantik for 470 points of damage.
        // [Fri Dec 28 23:31:14 2018] You shoot a Sebilisian bonecaster for 10717 points of damage. (Critical Double Bow Shot)
        // this has a negative look ahead to avoid matching obsolete nuke damage messages
        private static readonly Regex MeleeHitRegex = new Regex(@"^(.+?) (hit|shoot|kick|slash|crush|pierce|bash|slam|strike|punch|backstab|bite|claw|smash|slice|gore|maul|rend|burn|sting|sweep|learn|frenzy on|frenzies on)e?s? (?!by non-melee)(.+?) for (\d+) points? of damage\.(?:\s\(([^\(\)]+)\))?$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Fri Dec 28 15:43:53 2018] A lizard is burned by YOUR flames for 380 points of non-melee damage.
        // [Wed Apr 17 21:51:16 2019] An Iron Legion admirer is pierced by Garantik's thorns for 144 points of non-melee damage.
        // [Wed Jan 26 09:24:54 2022] YOU are burned by a syl ren sentinel's flames for 5158 points of non-melee damage!
        private static readonly Regex DamageShieldRegex = new Regex(@"^(.+?) (?:is|are) \w+ by (.+?) \w+ for (\d+) points? of non-melee damage[\.!]$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        
        // [Fri Jan 21 22:59:44 2022] Kaldolin was chilled to the bone for 1364 points of non-melee damage.
        private static readonly Regex ReverseDamageShieldRegex = new Regex(@"^(.+?) was chilled to the bone for (\d+) points? of non-melee damage\.$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // obsolete with 2019-02-12 test server patch
        // [Fri Dec 28 23:12:03 2018] Rumstil hit a scaled wolf for 726 points of non-melee damage.
        private static readonly Regex ObsoleteNukeDamageRegex = new Regex(@"^(.+?) hit (.+?) for (\d+) points? of non-melee damage\.(?:\s\(([^\(\)]+)\))?$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Wed Feb 13 21:31:06 2019] Tokiel hit a tirun crusher for 21617 points of chromatic damage by Mana Repetition Strike.
        private static readonly Regex NukeDamageRegex = new Regex(@"^(.+?) hit (.+?) for (\d+) points? of \w+ damage by (.+?)\.(?:\s\(([^\(\)]+)\))?$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Sun May 08 20:13:09 2016] An aggressive corpse has taken 212933 damage from your Mind Storm.
        // [Wed Feb 20 18:35:31 2019] A Drogan berserker has taken 34993 damage from Mind Tempest by Fourier.
        // [Wed Feb 20 18:35:31 2019] You have taken 52 damage from Chaos Claws by an ukun warhound's corpse.
        private static readonly Regex OwnDoTDamageRegex = new Regex(@"^(.+?) has taken (\d+) damage from your (.+?)\.(?:\s\(([^\(\)]+)\))?$", RegexOptions.Compiled);
        private static readonly Regex OtherDoTDamageRegex = new Regex(@"^(.+?) ha(?:s|ve) taken (\d+) damage from (.+?) by (.+?)\.(?:\s\(([^\(\)]+)\))?$", RegexOptions.Compiled);

        // DoTs from an dead/zoned caster 
        // Player has taken 125 damage by Curse of Rhag`Zadune.
        private static readonly Regex UnknownSourceDoTDamageRegex = new Regex(@"^(.+?) ha(?:s|ve) taken (\d+) damage (?:from|by) (.+?)\.(?:\s\(([^\(\)]+)\))?$", RegexOptions.Compiled);

        // doom spells and accidents (i don't think there is a 3rd party version of this)
        // [Thu Jul 09 20:52:57 2020] You hit yourself for 4017 points of unresistable damage by Bone Crunch.
        private static readonly Regex SelfDamageRegex = new Regex(@"^You hit yourself for (\d+) points .+? by (.+?)\.", RegexOptions.Compiled);


        public static LogHitEvent Parse(LogRawEvent e)
        {
            // this short-circuit exit is here strictly as a speed optimization 
            // this is the slowest parser of all and wasting time here slows down parsing quite a bit
            // "Bob has taken 1 damage" -- minimum possible occurance is at character 15?
            //if (e.Text.Length < 30 || e.Text.IndexOf("damage", 15) < 0)
            if (e.Text.IndexOf("damage", StringComparison.Ordinal) < 0)
                return null;

            LogEventMod mod = 0;
            var m = HitModRegex.Match(e.Text);
            if (m.Success)
            {
                mod = ParseMod(m.Groups[1].Value);
            }

            // rather than parsing self hits, we can use FixName to convert "yourself"
            //m = SelfDamageRegex.Match(e.Text);
            //if (m.Success)
            //{
            //    return new LogHitEvent()
            //    {
            //        Timestamp = e.Timestamp,
            //        Source = e.FixName(m.Groups[1].Value),
            //        Target = e.FixName(m.Groups[1].Value),
            //        Amount = Int32.Parse(m.Groups[2].Value),
            //        Spell = m.Groups[3].Value,
            //        Type = "self"
            //    };
            //}

            m = MeleeHitRegex.Match(e.Text);
            if (m.Success)
            {
                var type = m.Groups[2].Value;
                if (type == "frenzy on" || type == "frenzies on")
                    type = "frenzy";

                return new LogHitEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Type = type,
                    //Target = e.FixName(m.Groups[3].Value),
                    Target = m.Groups[3].Value == "himself" || m.Groups[3].Value == "herself" || m.Groups[3].Value == "itself" ? e.FixName(m.Groups[1].Value) : e.FixName(m.Groups[3].Value),
                    Amount = Int32.Parse(m.Groups[4].Value),
                    Mod = mod
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
                    Mod = mod
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
                    Mod = mod
                };
            }

            m = OtherDoTDamageRegex.Match(e.Text);
            if (m.Success)
            {
                if (m.Groups[1].Value == "You")
                    return new LogHitEvent()
                    {
                        Timestamp = e.Timestamp,
                        Target = e.FixName(m.Groups[4].Value),
                        Amount = Int32.Parse(m.Groups[2].Value),
                        Source = e.FixName(m.Groups[1].Value),
                        Spell = m.Groups[3].Value,
                        Type = "dot",
                        Mod = mod
                    };

                return new LogHitEvent()
                {
                    Timestamp = e.Timestamp,
                    Target = e.FixName(m.Groups[1].Value),
                    Amount = Int32.Parse(m.Groups[2].Value),
                    Source = e.FixName(m.Groups[4].Value),
                    Spell = m.Groups[3].Value,
                    Type = "dot",
                    Mod = mod
                };
            }

            m = UnknownSourceDoTDamageRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogHitEvent()
                {
                    Timestamp = e.Timestamp,
                    Target = e.FixName(m.Groups[1].Value),
                    Amount = Int32.Parse(m.Groups[2].Value),
                    Source = null,
                    Spell = m.Groups[3].Value,
                    Type = "dot",
                    Mod = mod
                };
            }

            m = DamageShieldRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogHitEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[2].Value),
                    Target = e.FixName(m.Groups[1].Value),
                    Amount = Int32.Parse(m.Groups[3].Value),
                    Type = "ds"
                };
            }

            m = ReverseDamageShieldRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogHitEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Target = e.FixName(m.Groups[1].Value),
                    Amount = Int32.Parse(m.Groups[2].Value),
                    Type = "ds"
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
                    Mod = mod
                };
            }

            return null;
        }

        private static LogEventMod ParseMod(string text)
        {
            LogEventMod mod = 0;
            text = text.ToLower();
            text = text.Replace("double bow shot", "doublebow"); // multi word mod won't split properly
            var parts = text.Split(' ');
            for (int i = 0; i < parts.Length; i++)
            {
                switch (parts[i])
                {
                    case "critical":
                    case "crippling":
                        mod |= LogEventMod.Critical;
                        break;
                    case "twincast":
                    case "twinstrike": 
                        mod |= LogEventMod.Twincast;
                        break;
                    case "lucky":
                        mod |= LogEventMod.Lucky;
                        break;
                    case "riposte":
                        mod |= LogEventMod.Riposte;
                        break;
                    case "strikethrough":
                        mod |= LogEventMod.Strikethrough;
                        break;
                    case "flurry":
                        mod |= LogEventMod.Flurry;
                        break;
                    case "finishing":
                        mod |= LogEventMod.Finishing_Blow;
                        break;
                    case "doublebow":
                        mod |= LogEventMod.Double_Bow_Shot;
                        break;
                    case "headshot":
                        mod |= LogEventMod.Headshot;
                        break;
                    case "assassinate":
                        mod |= LogEventMod.Assassinate;
                        break;
                    case "slay":
                        mod |= LogEventMod.Slay_Undead;
                        mod |= LogEventMod.Critical;
                        break;
                    case "rampage":
                        mod |= LogEventMod.Rampage;
                        break;
                    case "wild":
                        mod |= LogEventMod.Wild_Rampage;
                        break;
                    case "locked":
                        mod |= LogEventMod.Locked;
                        break;
                    default:
                        break;
                }
            }
            return mod;
        }

    }
}
