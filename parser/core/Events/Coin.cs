using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EQLogParser
{
    /// <summary>
    /// Generated when a player receives coin.
    /// </summary>
    public class LogCoinEvent : LogEvent
    {
        public int Platinum;
        public int Gold;
        public int Silver;
        public int Copper;
        //public bool Split;

        public override string ToString()
        {
            return String.Format("Coin: {0}.{1}{2}{3}", Platinum, Gold, Silver, Copper);
        }

        // [Wed Dec 08 20:29:14 2021] Alive group members received 144 platinum and 4 silver as their share of the split from the corpse.
        // [Sun Jan 02 11:41:53 2022] You receive 15 platinum and 7 gold from the corpse.
        // [Mon Dec 20 08:59:37 2021] You receive 78 platinum, 1 gold, 7 silver and 1 copper as your split (with a lucky bonus).
        // [Thu Dec 30 20:52:57 2021] You receive 9 gold as your split.
        private static readonly Regex LootRegex = new Regex(@"^(You receive \d|Alive group members received \d)", RegexOptions.Compiled);
        private static readonly Regex AmountRegex = new Regex(@"(\d+) (platinum|gold|silver|copper)", RegexOptions.Compiled);

        // mission reward
        // [Tue Dec 21 22:05:13 2021] You receive 318 platinum.


        public static LogCoinEvent Parse(LogRawEvent e)
        {
            var m = LootRegex.Match(e.Text);
            if (m.Success)
            {
                var amounts = AmountRegex.Matches(e.Text);
                return new LogCoinEvent
                {
                    Timestamp = e.Timestamp,
                    Platinum = Int32.Parse(amounts.FirstOrDefault(x => x.Groups[2].Value == "platinum")?.Groups[1]?.Value ?? "0"),
                    Gold = Int32.Parse(amounts.FirstOrDefault(x => x.Groups[2].Value == "gold")?.Groups[1]?.Value ?? "0"),
                    Silver = Int32.Parse(amounts.FirstOrDefault(x => x.Groups[2].Value == "silver")?.Groups[1]?.Value ?? "0"),
                    Copper = Int32.Parse(amounts.FirstOrDefault(x => x.Groups[2].Value == "copper")?.Groups[1]?.Value ?? "0"),
                    //Split = e.Text.Contains("split") // true even if grouped with merc
                };

                //var coin = new LogCoinEvent
                //{
                //    Timestamp = e.Timestamp
                //};
                //foreach (Match amount in amounts)
                //{
                //    if (amount.Groups[2].Value == "platinum")
                //        coin.Platinum = Int32.Parse(amount.Groups[1].Value);
                //}
            }

            return null;
        }

    }
}
