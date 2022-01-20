using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/*
2021-01-12
- A message is now displayed when loot is left on a corpse after expiring from the Advanced Loot Window

*/

namespace EQLogParser
{
    /// <summary>
    /// Generated when an item is left on a corpse.
    /// </summary>
    public class LogRotEvent : LogEvent
    {
        public string Item;
        public string Char;
        public string Source;
        //public int Qty;

        public override string ToString()
        {
            return String.Format("Item: {0} rotted", Item);
        }

        // [Tue Jan 12 18:02:50 2021] --You left 2 Deepwater Ink on a great white shark.--
        // [Tue Jan 12 18:53:17 2021] --You left a Medium Quality Snake Skin on a female Darkhollow redback.--
        private static readonly Regex ItemRotRegex = new Regex(@"^--(\w+) left (an?|\d+) ([^\.]+) on ([^\.]+)\s?\.--$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Wed Jan 12 19:00:33 2022] --a Sarnak Blood was left on an a sarnak consc's corpse.--
        private static readonly Regex ItemUnclaimedRegex = new Regex(@"^--(?:an?|\d+) ([^\\]+) (?:was|were) left on ([^\.]+)\.--$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        public static LogRotEvent Parse(LogRawEvent e)
        {
            var m = ItemRotRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogRotEvent
                {
                    Timestamp = e.Timestamp,
                    Char = e.FixName(m.Groups[1].Value),
                    Item = m.Groups[3].Value.Trim(),
                    Source = e.FixName(m.Groups[4].Value.Replace("'s corpse", "")),
                    //Qty = System.Char.IsDigit(m.Groups[2].Value[0]) ? Int32.Parse(m.Groups[2].Value) : 1,
                };
            }

            m = ItemUnclaimedRegex.Match(e.Text);
            if (m.Success)
            {
                return new LogRotEvent
                {
                    Timestamp = e.Timestamp,
                    Item = m.Groups[1].Value.Trim(),
                    Source = e.FixName(m.Groups[2].Value.Replace("'s corpse", "")),
                };
            }

            return null;
        }

    }
}
