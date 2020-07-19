using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when a player crafts an item.
    /// </summary>
    public class LogCraftEvent : LogEvent
    {
        public string Item;
        public string Char;

        public override string ToString()
        {
            return String.Format("Craft: {0} {1}", Item, Char);
        }

        // [Fri Jun 10 08:39:54 2016] You can no longer advance your skill from making this item.
        // [Fri Jun 10 08:39:54 2016] You lacked the skills to fashion the items together.
        // [Fri Jun 10 08:39:54 2016] You have fashioned the items together to create something new: Magi-potent Crystal.
        private static readonly Regex ItemCraftedRegex = new Regex(@"^You have fashioned the items together to create [^:]+: ([^\.]+)\.$", RegexOptions.Compiled);

        public static LogCraftEvent Parse(LogRawEvent e)
        {
            var m = ItemCraftedRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogCraftEvent
                {
                    Timestamp = e.Timestamp,
                    Char = e.Player,
                    Item = m.Groups[1].Value
                };
            }

            return null;
        }

    }
}
