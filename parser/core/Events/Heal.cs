using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/*
2018-11-14
- Healing spells now report actual and potential healing to all players in the area. Added an 
additional chat filter option to turn off heals that land for zero.

2019-03-05
- Percentage heal spells now display their name in healing messages.

*/

namespace EQLogParser
{
    /// <summary>
    /// Generated when a heal lands on someone.
    /// </summary>
    public class LogHealEvent : LogEvent
    {
        public string Source;
        public string Target;
        public int Amount;
        public int FullAmount;
        public string Spell;
        public LogEventMod Mod;
        
        public override string ToString()
        {
            return String.Format("Heal: {0} => {1} ({2})", Source, Target, Amount);
        }

        private static readonly Regex HealModRegex = new Regex(@"\(([^\(\)]+)\)?$", RegexOptions.Compiled | RegexOptions.RightToLeft);


        // [Sun Jan 13 23:09:15 2019] Uteusher healed you over time for 1797 hit points by Devout Elixir.
        // [Sun Jan 13 23:09:15 2019] Uteusher healed you over time for 208 (1797) hit points by Devout Elixir.
        // [Sun Jan 13 23:09:15 2019] Prime healed Blurr over time for 1049 hit points by Prophet's Gift of the Ruchu. (Lucky Critical)
        private static readonly Regex HoTRegex = new Regex(@"^(.+?) healed (.+?) over time for (\d+)(?: \((\d+)\))? hit points by (.+?)\.(?: \((.+?)\))?$", RegexOptions.Compiled);

        // [Sun Jan 13 23:09:15 2019] Uteusher has been healed over time for 0 (900) hit points by Celestial Regeneration XVIII.
        // [Mon Mar 18 23:55:00 2019] You have been healed over time for 9525 hit points by Merciful Elixir Rk. II.
        private static readonly Regex HoTRegexNoSource = new Regex(@"^(\w+) ha(?:s|ve) been healed over time for (\d+)(?: \((\d+)\))? hit points by (.+?)\.(?: \((.+?)\))?$", RegexOptions.Compiled);

        // [Tue Aug 25 08:39:45 2020] You healed Rumstil for 4636 hit points by Vampiric Consumption.
        // [Mon Sep 14 20:43:54 2020] Saity healed you for 62604 (82107) hit points by Sincere Light Rk. II.
        private static readonly Regex InstantRegex = new Regex(@"^(.+?) healed (.+?) for (\d+)(?: \((\d+)\))? hit points(?: by (.+?))?\.(?: \((.+?)\))?$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // prior to yyyy-mm-dd the emote and heal message were combined on a single line
        // the obsolete regex still mostly works except in cases where the healer has a multi-word name
        // [Sun Jan 13 23:09:15 2019] You wither under a vampiric strike. You healed Rumstil for 668 (996) hit points by Vampiric Strike VIII.
        // [Sun Jan 13 23:09:15 2019] Lenantik is bathed in a devout light. Uteusher healed Lenantik for 2153 (9875) hit points by Devout Light.
        // [Sun Jan 13 23:09:15 2019] Lenantik is strengthened by Darkpaw spirits. You healed Lenantik for 1216 (1248) hit points by Darkpaw Focusing.
        // [Sun Jan 13 23:09:15 2019] A holy light surrounds you. You healed Rumstil for 361 hit points by HandOfHolyVengeanceVRecourse. (Critical)
        // [Sun Jan 13 23:09:15 2019] Xebn is healed by life-giving energy. Brugian healed Xebn for 84056 hit points by Furial Renewal.
        // [Sun Jan 13 23:09:15 2019] You healed Rumstil for 8 hit points by Blood of the Devoted.
        private static readonly Regex ObsoleteInstantRegex = new Regex(@"\. (.+?) healed (.+?) for (\d+)(?: \((\d+)\))? hit points(?: by (.+?))?\.(?: \((.+?)\))?$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        public static LogHealEvent Parse(LogRawEvent e)
        {
            // this short-circuit exit is here strictly as a speed optmization 
            if (e.Text.IndexOf("heal", StringComparison.Ordinal) < 0)
                return null;

            LogEventMod mod = 0;
            var m = HealModRegex.Match(e.Text);
            if (m.Success)
            {
                mod = ParseMod(m.Groups[1].Value);
            }

            m = HoTRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogHealEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Target = m.Groups[2].Value == "himself" || m.Groups[2].Value == "herself" || m.Groups[2].Value == "itself" ? e.FixName(m.Groups[1].Value) : e.FixName(m.Groups[2].Value),
                    Amount = Int32.Parse(m.Groups[3].Value),
                    FullAmount = m.Groups[4].Success ? Int32.Parse(m.Groups[4].Value) : Int32.Parse(m.Groups[3].Value),
                    Spell = m.Groups[5].Success ? m.Groups[5].Value : null,
                    Mod = mod
                };
            }

            m = HoTRegexNoSource.Match(e.Text);
            if (m.Success)
            {
                return new LogHealEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = null,
                    Target = e.FixName(m.Groups[1].Value),
                    Amount = Int32.Parse(m.Groups[2].Value),
                    FullAmount = m.Groups[3].Success ? Int32.Parse(m.Groups[3].Value) : Int32.Parse(m.Groups[2].Value),
                    Spell = m.Groups[4].Success ? m.Groups[4].Value : null,
                    Mod = mod
                };
            }

            // InstantRegex will incorrectly capture the obsolete messages 
            // the check for the '.' used to exclude them
            m = InstantRegex.Match(e.Text);
            if (m.Success && !m.Groups[1].Value.Contains('.'))
            {
                return new LogHealEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Target = m.Groups[2].Value == "himself" || m.Groups[2].Value == "herself" || m.Groups[2].Value == "itself" ? e.FixName(m.Groups[1].Value) : e.FixName(m.Groups[2].Value),
                    Amount = Int32.Parse(m.Groups[3].Value),
                    FullAmount = m.Groups[4].Success ? Int32.Parse(m.Groups[4].Value) : Int32.Parse(m.Groups[3].Value),
                    Spell = m.Groups[5].Success ? m.Groups[5].Value : null,
                    Mod = mod
                };
            }

            m = ObsoleteInstantRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogHealEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Target = m.Groups[2].Value == "himself" || m.Groups[2].Value == "herself" || m.Groups[2].Value == "itself" ? e.FixName(m.Groups[1].Value) : e.FixName(m.Groups[2].Value),
                    Amount = Int32.Parse(m.Groups[3].Value),
                    FullAmount = m.Groups[4].Success ? Int32.Parse(m.Groups[4].Value) : Int32.Parse(m.Groups[3].Value),
                    Spell = m.Groups[5].Success ? m.Groups[5].Value : null,
                    Mod = mod
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
                    case "critical":
                        mod |= LogEventMod.Critical;
                        break;
                    case "twincast":
                        mod |= LogEventMod.Twincast;
                        break;
                    case "lucky":
                        mod |= LogEventMod.Lucky;
                        break;
                    default:
                        break;
                }
            }
            return mod;
        }

    }
}
