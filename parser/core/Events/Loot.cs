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
        public string Looter;

        public override string ToString()
        {
            return String.Format("Item: {0} looted by {1}", Item, Looter);
        }

        // [Tue Apr 26 20:17:58 2016] --Rumstil has looted a Alluring Flower.--
        // [Tue Apr 26 20:26:20 2016] --You have looted a Bixie Chitin Sword.--
        private static readonly Regex ItemLootedRegex = new Regex(@"^--(\w+) (?:has|have) looted (.+?)\.--$", RegexOptions.Compiled);

        // [Fri Jun 10 08:39:54 2016] You can no longer advance your skill from making this item.
        // [Fri Jun 10 08:39:54 2016] You lacked the skills to fashion the items together.
        // [Fri Jun 10 08:39:54 2016] You have fashioned the items together to create something new: Magi-potent Crystal.
        private static readonly Regex ItemCraftedRegex = new Regex(@"^You have fashioned the items together to create .+?: (.+?)\.$", RegexOptions.Compiled);

        public static LogLootEvent Parse(LogRawEvent e)
        {
            var m = ItemLootedRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogLootEvent
                {
                    Timestamp = e.Timestamp,
                    Looter = e.FixName(m.Groups[1].Value),
                    Item = m.Groups[2].Value
                };
            }


            m = ItemCraftedRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogLootEvent
                {
                    Timestamp = e.Timestamp,
                    Looter = e.Player,
                    Item = m.Groups[1].Value
                };
            }

            return null;
        }

    }
}
