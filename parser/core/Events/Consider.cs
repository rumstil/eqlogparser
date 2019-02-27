using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when a /consider response is shown.
    /// </summary>
    public class LogConEvent : LogEvent
    {
        public string Name;
        public string Faction;
        public string Strength;
        public int Level;
        public bool Rare;

        public override string ToString()
        {
            return String.Format("Consider: {0} ({1})", Name, Level);
        }

        // [Fri Dec 28 16:33:01 2018] A grizzly bear glares at you threateningly -- looks kind of dangerous. (Lvl: 74)
        // [Fri Dec 28 16:23:01 2018] Herald of Druzzil Ro regards you indifferently -- what would you like your tombstone to say? (Lvl: 90)
        // http://www.zlizeq.com/Game_Mechanics-Faction_and_Consider
        private static readonly Regex ConRegex = new Regex(@"(.+)( - \<.+)? ((?:scowls|glares|glowers|regards|looks|judges|kindly|looks) .+?) -- (.+) \(Lvl: (\d+)\)$", RegexOptions.RightToLeft | RegexOptions.Compiled);

        public static LogConEvent Parse(LogRawEvent e)
        {
            var m = ConRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogConEvent
                {
                    Timestamp = e.Timestamp,
                    Name = m.Groups[1].Value,
                    Faction = m.Groups[3].Value,
                    Strength = m.Groups[4].Value,
                    Level = Int32.Parse(m.Groups[5].Value),
                    Rare =  m.Groups[2].Success
                };
            }

            return null;
        }

    }
}
