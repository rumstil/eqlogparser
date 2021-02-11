using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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

        // [Sun Jul 12 22:32:45 2020] No one was interested in the 1 item(s): Glowing Sebilisian Boots. These items can be randomed again or will be available to everyone after the corpse unlocks.
        private static readonly Regex ItemUnclaimedRegex = new Regex(@"^No one was interested in the .+: (.+)\. These items", RegexOptions.Compiled);

        // [Wed Apr 17 21:52:43 2019] 2 item(s): Energy Core given to Fourier. It has been removed from your Shared Loot List.
        // [Sat Sep 28 16:17:01 2019] A Copper-Melded Faceplate was given to Fourier.
        // [Sat Sep 28 18:14:57 2019] A Summoner's Trinket of Insecurity was given to you.

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
                };
            }

            return null;
        }

    }
}
