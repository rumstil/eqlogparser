using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when a player loots a corpse.
    /// </summary>
    public class LogLootEvent : LogEvent
    {
        public string Item;
        public string Char;
        public string Source;
        public int Qty;

        public override string ToString()
        {
            return String.Format("Item: {0} looted by {1}", Item, Char);
        }

        // [Tue Apr 26 20:17:58 2016] --Rumstil has looted a Alluring Flower.--
        // [Tue Apr 26 20:26:20 2016] --You have looted a Bixie Chitin Sword.--
        // [Wed Apr 17 21:52:43 2019] --Rumstil has looted 2 Energy Core.--
        // [Thu Dec 19 18:59:56 2019] --You have looted a Viable Chokidai Egg from a chokidai egg sac .--
        private static readonly Regex ItemLootedRegex = new Regex(@"^--(\w+) \w+ looted (an?|\d+) ([^\.]+)(?:from ([^\.]+))?\s?\.--$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Thu Jan 14 20:40:08 2021] Dude grabbed a Restless Ice Cloth Legs Ornament from an icebound chest .
        private static readonly Regex ItemGrabbedRegex = new Regex(@"^(\w+) grabbed a (.+) from ([^\.]+?)\s?\.$", RegexOptions.Compiled);


        public static LogLootEvent Parse(LogRawEvent e)
        {
            var m = ItemLootedRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogLootEvent
                {
                    Timestamp = e.Timestamp,
                    Char = e.FixName(m.Groups[1].Value),
                    Item = m.Groups[3].Value.Trim(),
                    Source = e.FixName(m.Groups[4].Value.Replace("'s corpse", "")),
                    Qty = System.Char.IsDigit(m.Groups[2].Value[0]) ? Int32.Parse(m.Groups[2].Value) : 1,
                };
            }

            m = ItemGrabbedRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogLootEvent
                {
                    Timestamp = e.Timestamp,
                    Char = e.FixName(m.Groups[1].Value),
                    Item = m.Groups[2].Value.Trim(),
                    Source = e.FixName(m.Groups[3].Value.Replace("'s corpse", "").TrimEnd()),
                    Qty = 1,
                };
            }

            return null;
        }

    }
}
