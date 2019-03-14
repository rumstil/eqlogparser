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
        public int GrossAmount;
        public string Spell;
        public string Special;

        public override string ToString()
        {
            return String.Format("Heal: {0} => {1} ({2})", Source, Target, Amount);
        }

        // [Sun Jan 13 23:09:15 2019] Uteusher healed you over time for 1797 hit points by Devout Elixir.
        // [Sun Jan 13 23:09:15 2019] Uteusher healed you over time for 208 (1797) hit points by Devout Elixir.
        // [Sun Jan 13 23:09:15 2019] Prime healed Blurr over time for 1049 hit points by Prophet's Gift of the Ruchu. (Lucky Critical)
        private static readonly Regex HoTRegex = new Regex(@"^(\w+) healed (.+?) over time for (\d+)(?: \((\d+)\))? hit points by (.+?)\.(?: \((.+?)\))?$", RegexOptions.Compiled);

        // [Sun Jan 13 23:09:15 2019] Uteusher has been healed over time for 0 (900) hit points by Celestial Regeneration XVIII.
        private static readonly Regex HoTRegexNoSource = new Regex(@"^(\w+) has been healed over time for (\d+)(?: \((\d+)\))? hit points by (.+?)\.(?: \((.+?)\))?$", RegexOptions.Compiled);
        
        // [Sun Jan 13 23:09:15 2019] You wither under a vampiric strike. You healed Rumstil for 668 (996) hit points by Vampiric Strike VIII.
        // [Sun Jan 13 23:09:15 2019] Lenantik is bathed in a devout light. Uteusher healed Lenantik for 2153 (9875) hit points by Devout Light.
        // [Sun Jan 13 23:09:15 2019] Lenantik is strengthened by Darkpaw spirits. You healed Lenantik for 1216 (1248) hit points by Darkpaw Focusing.
        // [Sun Jan 13 23:09:15 2019] A holy light surrounds you. You healed Rumstil for 361 hit points by HandOfHolyVengeanceVRecourse. (Critical)
        // [Sun Jan 13 23:09:15 2019] Xebn is healed by life-giving energy. Brugian healed Xebn for 84056 hit points by Furial Renewal.
        // [Sun Jan 13 23:09:15 2019] You healed Rumstil for 8 hit points by Blood of the Devoted.
        private static readonly Regex InstantRegex = new Regex(@"(?:^|\. )([\w\s]+) healed (.+?) for (\d+)(?: \((\d+)\))? hit points(?: by (.+?))?\.(?: \((.+?)\))?$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Sun Jan 13 23:09:15 2019] Blurr healed itself for 12 (617) hit points.


        public static LogHealEvent Parse(LogRawEvent e)
        {
            var m = HoTRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogHealEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Target = m.Groups[2].Value == "himself" || m.Groups[2].Value == "herself" || m.Groups[2].Value == "itself" ? e.FixName(m.Groups[1].Value) : e.FixName(m.Groups[2].Value),
                    Amount = Int32.Parse(m.Groups[3].Value),
                    GrossAmount = m.Groups[4].Success ? Int32.Parse(m.Groups[4].Value) : Int32.Parse(m.Groups[3].Value),
                    Spell = m.Groups[5].Success ? m.Groups[5].Value : null,
                    Special = m.Groups[6].Success ? m.Groups[6].Value.ToLower() : null
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
                    GrossAmount = m.Groups[3].Success ? Int32.Parse(m.Groups[3].Value) : Int32.Parse(m.Groups[2].Value),
                    Spell = m.Groups[4].Success ? m.Groups[4].Value : null,
                    Special = m.Groups[5].Success ? m.Groups[5].Value.ToLower() : null
                };
            }

            m = InstantRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogHealEvent()
                {
                    Timestamp = e.Timestamp,
                    Source = e.FixName(m.Groups[1].Value),
                    Target = m.Groups[2].Value == "himself" || m.Groups[2].Value == "herself" || m.Groups[2].Value == "itself" ? e.FixName(m.Groups[1].Value) : e.FixName(m.Groups[2].Value),
                    Amount = Int32.Parse(m.Groups[3].Value),
                    GrossAmount = m.Groups[4].Success ? Int32.Parse(m.Groups[4].Value) : Int32.Parse(m.Groups[3].Value),
                    Spell = m.Groups[5].Success ? m.Groups[5].Value : null,
                    Special = m.Groups[6].Success ? m.Groups[6].Value.ToLower() : null
                };
            }

            return null;
        }

    }
}
