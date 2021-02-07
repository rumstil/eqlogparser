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
        /// A list of global or common drops that can be ignored.
        /// </summary>
        public readonly HashSet<string> Ignore = new HashSet<string>
        {
            "Aderirse Bur",
            "Alkalai Loam",
            "Amber",
            "Animal Venom",
            "Black Pearl",
            "Black Powder Pouch",
            "Black Sapphire",
            "Bonded Loam",
            "Burnt Out Golem Animation Essence",
            "Chronal Resonance Dust",
            "Cloth Bolt",
            "Cobalt Ore",
            "Complex Gold Gem Dusted Rune",
            "Complex Platinum Gem Dusted Rune",
            "Complex Velium Gem Dusted Rune",
            "Crystallized Sulfur",
            "Dirty Runic Papyrus",
            "Dream Dust",
            "Dweric Powder",
            "Emerald",
            "Excellent Silk",
            "Exquisite Animal Pelt",
            "Exquisite Gem Dusted Rune",
            "Exquisite Gold Gem Dusted Rune",
            "Exquisite Platinum Gem Dusted Rune",
            "Exquisite Silk",
            "Exquisite Velium Gem Dusted Rune",
            "Extra Planar Potential Shard",
            "Fantastic Animal Pelt",
            "Fantastic Silk",
            "Fine Feathers",
            "Fine Silk",
            "Fine Steel Dagger",
            "Fine Steel Great Staff",
            "Fine Steel Long Sword",
            "Fine Steel Morning Star",
            "Fine Steel Rapier",
            "Fine Steel Scimitar",
            "Fine Steel Short Sword",
            "Fine Steel Spear",
            "Fine Steel Two Handed Sword",
            "Fine Steel Warhammer",
            "Fire Emerald",
            "Fire Opal",
            "Flame of Vox",
            "Flawless Animal Pelt",
            "Flawless Silk",
            "Fresh Meat",
            "Glove of Rallos Zek",
            "Gold Gem Dusted Rune",
            "Golem Animation Essence",
            "Grimy Fine Vellum Parchment",
            "Grimy Papyrus",
            "Grubby Fine Papyrus",
            "Grubby Fine Parchment",
            "Harmonagate",
            "Ice of Velious",
            "Immaculate Animal Pelt",
            "Immaculate Silk",
            "Jacinth",
            "Jade",
            "Leather Roll",
            "Lumber Plank",
            "Malachite",
            "Medicinal Herbs",
            "Natural Spices",
            "Nightmare Ruby",
            "Opal",
            "Osmium Ore",
            "Pearl",
            "Peridot",
            "Phosphorous Powder",
            "Platinum Gem Dusted Rune",
            "Prestidigitase",
            "Pristine Larkspur",
            "Pristine Oleander",
            "Pure Diamond Trade Gem",
            "Purified Grade AA Gormar Venom",
            "Purified Grade AA Nigriventer Venom",
            "Raw Amber Nihilite",
            "Raw Crimson Nihilite",
            "Raw Faycite Crystal",
            "Raw Fine Supple Runic Hide",
            "Raw Indigo Nihilite",
            "Raw Shimmering Nihilite",
            "Rhenium Ore",
            "Rotting Fang",
            "Rune Binding Powder",
            "Rune of Concussion",
            "Rune of Crippling",
            "Rune of Impulse",
            "Rune of Rathe",
            "Saltpeter",
            "Scale Ore",
            "Shabby Vellum Parchment",
            "Sibilisan Viridian Pigment",
            "Smudged Rough Papyrus",
            "Sooty Fine Runic Papyrus",
            "Spider Legs",
            "Spider Silk",
            "Spider Venom Sac",
            "Spongy Loam",
            "Stained Fine Runic Spell Scroll",
            "Staurolite",
            "Steel Ingot",
            "Stone of Tranquility",
            "Sunshard Ore",
            "Sunshard Pebble",
            "Superb Silk",
            "Taaffeite",
            "Tantalum Ore",
            "Tears of Prexus",
            "Titanium Ore",
            "Tungsten Ore",
            "Ukun Hide",
            "Uncut Alexandrite",
            "Uncut Amethyst",
            "Uncut Black Sapphire",
            "Uncut Combine Star",
            "Uncut Demantoid",
            "Uncut Goshenite",
            "Uncut Jacinth",
            "Uncut Morganite",
            "Uncut Rubellite",
            "Urticaceae",
            "Vanadium Ore",
            "Velium Gem Dusted Rune",
            "Velium Gemmed Rune",
            "Velium Silvered Rune",
            "Words of Cazic-Thule",
            "Words of Crippling Force",
            "Words of Incarceration",
            "Words of Obliteration",
            "Words of Possession",
            "Writing Ink",
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
                if (!Ignore.Contains(loot.Item, StringComparer.Ordinal) && zone != null)
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
