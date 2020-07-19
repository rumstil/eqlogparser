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

        // http://www.zlizeq.com/Game_Mechanics-Faction_and_Consider
        //... scowls at you, ready to attack
        //... glares at you threateningly
        //... glowers at you dubiously
        //... looks your way apprehensively	
        //... regards you indifferently	
        //... judges you amiably	
        //... kindly considers you	
        //... looks upon you warmly	
        //... regards you as an ally

        // [Fri Dec 28 16:33:01 2018] A grizzly bear glares at you threateningly -- looks kind of dangerous. (Lvl: 74)
        // [Fri Dec 28 16:23:01 2018] Herald of Druzzil Ro regards you indifferently -- what would you like your tombstone to say? (Lvl: 90)
        // [Fri Dec 28 16:23:01 2018] Cadcane the Unmourned - a rare creature - scowls at you, ready to attack -- what would you like your tombstone to say? (Lvl: 118)
        // TODO: Is the level always shown or hidden sometimes?
        private static readonly Regex ConRegex = new Regex(@"(.+)( -.+)? ((?:scowls|glares|glowers|regards|looks|judges|kindly) .+?) -- (.+) \(Lvl: (\d+)\)$", RegexOptions.RightToLeft | RegexOptions.Compiled);
        
        // [Fri Dec 28 16:23:01 2018] Roon - </c><c \"#E1B511\">a rare creature</c><c \"#00F0F0\"> - scowls at you, ready to attack -- looks kind of dangerous. (Lvl: 104)
        private static readonly Regex ObsoleteConRegex = new Regex(@"(.+)( - \<.+)? ((?:scowls|glares|glowers|regards|looks|judges|kindly) .+?) -- (.+) \(Lvl: (\d+)\)$", RegexOptions.RightToLeft | RegexOptions.Compiled);

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
