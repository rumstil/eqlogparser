using EQLogParser.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace EQLogParser
{
    public delegate void LootTrackerEvent(LootInfo args);

    public class LootInfo
    {
        public string Item { get; set; }

        /// <summary>
        /// Mob will be "Unknown" if item rotted or came from an unknown mob.
        /// Mob will be "Crafted" if item was a crafted tradeskill item.
        /// </summary>
        public string Mob { get; set; }

        /// <summary>
        /// Zone will be null if item was crafted.
        /// </summary>
        public string Zone { get; set; }

        /// <summary>
        /// Server this was looted on. Mostly interesting if test/beta.
        /// </summary>
        public string Server { get; set; }

        public DateTime Date { get; set; }
    }

    /// <summary>
    /// Track loot drops.
    /// </summary>
    public class LootTracker
    {
        /// <summary>
        /// A list of global drops. Obviously this needs to be expanded.
        /// </summary>
        public List<string> Ignore = new List<string> 
        {
            "Bone Chips",
            "Diamond",
            "Blue Diamond",
            "Star Ruby",
            "Sapphire",
            "Ruby",
        };

        private string zone;
        private string server;

        public event LootTrackerEvent OnLoot;

        public void HandleEvent(LogEvent e)
        {
            if (e is LogOpenEvent open)
            {
                server = open.Server;
                if (server != "test" && server != "beta")
                    server = null;
            }

            if (e is LogLootEvent loot)
            {
                if (!Ignore.Contains(loot.Item, StringComparer.Ordinal))
                    OnLoot?.Invoke(new LootInfo { Item = loot.Item, Mob = loot.Source ?? "Unknown", Zone = zone, Date = e.Timestamp });
            }

            // i'm tempted to ignore rot gear, but it may be worth tracking for tradeskill items?
            //if (e is LogRotEvent rot)
            //{
            //    if (!IgnoredItems.Contains(rot.Item))
            //        OnLoot?.Invoke(new LootInfo { Item = rot.Item, Mob = "Unknown", Zone = currentZone, Date = e.Timestamp });
            //}

            //if (e is LogCraftEvent craft)
            //{
            //    OnLoot?.Invoke(new LootInfo { Item = craft.Item, Mob = "Crafted", Date = e.Timestamp });
            //}

            if (e is LogZoneEvent z)
            {
                zone = z.Name;
            }
        }

    }
}
